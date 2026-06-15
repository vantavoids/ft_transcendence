using Guild.Domain.Results;

namespace Guild.Domain.Guild;

public sealed class Guild
{
	public const int MaxNameLen = 100;
	public const int MaxUrlLen = 512;
	public const int MaxRoles = 250;

	private readonly List<Role> _roles = [];
	private readonly List<GuildMember> _members = [];
	private readonly List<MemberRole> _memberRoles = [];

	// EF Core constructor
	private Guild() { }

	private Guild(
		long id,
		string name,
		string? description,
		string? iconUrl,
		string? bannerUrl,
		long ownerId,
		DateTimeOffset createdAt,
		DateTimeOffset updatedAt)
	{
		Id = id;
		Name = name;
		Description = description;
		IconUrl = iconUrl;
		BannerUrl = bannerUrl;
		OwnerId = ownerId;
		CreatedAt = createdAt;
		UpdatedAt = updatedAt;
	}

	public long Id { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string? Description { get; private set; }
	public string? IconUrl { get; private set; }
	public string? BannerUrl { get; private set; }
	public long OwnerId { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }

	public IReadOnlyCollection<Role> Roles => _roles;
	public IReadOnlyCollection<GuildMember> Members => _members;
	public IReadOnlyCollection<MemberRole> MemberRoles => _memberRoles;

	public static Result<Guild> Create(
		long id,
		string? name,
		string? description,
		string? iconUrl,
		string? bannerUrl,
		long ownerId,
		long everyoneRoleId,
		long adminRoleId,
		DateTimeOffset now)
	{
		var validationResult = ValidateSettings(name, description, iconUrl, bannerUrl);
		if (validationResult.IsFailure)
			return validationResult.Error;

		var guild = new Guild(
			id: id,
			name: name!,
			description: description,
			iconUrl: iconUrl,
			bannerUrl: bannerUrl,
			ownerId: ownerId,
			createdAt: now,
			updatedAt: now);

		var everyone = Role.CreateEveryone(everyoneRoleId, id, now);
		var admin = Role.CreateAdministrator(adminRoleId, id, now);
		guild._roles.Add(everyone);
		guild._roles.Add(admin);

		var ownerMemberResult = GuildMember.Create(id, ownerId, now);
		if (ownerMemberResult.IsFailure)
			return ownerMemberResult.Error;
		guild._members.Add(ownerMemberResult.Value);

		guild._memberRoles.Add(MemberRole.Create(id, ownerId, adminRoleId, now));

		return guild;
	}

	public Result UpdateSettings(
		string? name,
		string? description,
		string? iconUrl,
		string? bannerUrl,
		DateTimeOffset now)
	{
		// treat null as "no change", empty string as "clear" for nullable fields
		var newName = name ?? Name;
		var newDescription = description is null ? Description : (description.Length == 0 ? null : description);
		var newIconUrl = iconUrl is null ? IconUrl : (iconUrl.Length == 0 ? null : iconUrl);
		var newBannerUrl = bannerUrl is null ? BannerUrl : (bannerUrl.Length == 0 ? null : bannerUrl);

		var validationResult = ValidateSettings(newName, newDescription, newIconUrl, newBannerUrl);
		if (validationResult.IsFailure)
			return validationResult;

		Name = newName;
		Description = newDescription;
		IconUrl = newIconUrl;
		BannerUrl = newBannerUrl;
		UpdatedAt = now;
		return Result.Ok();
	}

	public Result TransferOwnership(long newOwnerId, DateTimeOffset now)
	{
		if (newOwnerId <= 0)
			return GuildFailures.TargetNotAMember;

		// caller must ensure the new owner is a current member; the handler enforces it
		OwnerId = newOwnerId;
		UpdatedAt = now;
		return Result.Ok();
	}

	public Result<GuildMember> AddMember(long userId, DateTimeOffset now)
	{
		if (_members.Any(m => m.UserId == userId))
			return GuildFailures.AlreadyMember;

		var memberResult = GuildMember.Create(Id, userId, now);
		if (memberResult.IsFailure)
			return memberResult.Error;

		_members.Add(memberResult.Value);
		UpdatedAt = now;
		return memberResult.Value;
	}

	public Result RemoveMember(long userId, DateTimeOffset now)
	{
		if (userId == OwnerId)
			return GuildFailures.OwnerCannotLeave;

		var member = _members.FirstOrDefault(m => m.UserId == userId);
		if (member is null)
			return GuildFailures.NotAMember;

		_members.Remove(member);
		_memberRoles.RemoveAll(mr => mr.UserId == userId);
		UpdatedAt = now;
		return Result.Ok();
	}

	public Result<GuildMember> UpdateMemberNickname(long userId, string? nickname)
	{
		var member = _members.FirstOrDefault(m => m.UserId == userId);
		if (member is null)
			return GuildFailures.TargetNotAMember;

		var updateResult = member.UpdateNickname(nickname);
		if (updateResult.IsFailure)
			return updateResult.Error;

		return member;
	}

	/// <summary>
	/// appends a new custom role at the top of the position stack
	/// (max existing position + 1). the @everyone and Administrator roles
	/// seeded by <see cref="Create"/> mean <c>_roles</c> is never empty here.
	/// permission grants are not authorized here; the handler does the
	/// "can't grant permissions you lack" check before calling this
	/// </summary>
	public Result<Role> AddRole(
		long roleId,
		string? name,
		string? color,
		long permissions,
		bool isHoisted,
		bool isMentionable,
		DateTimeOffset now)
	{
		if (permissions < 0)
			return GuildFailures.RolePermissionsInvalid;

		if (_roles.Count >= MaxRoles)
			return GuildFailures.MaxRolesReached;

		var maxPosition = _roles.Max(r => r.Position);
		if (maxPosition == int.MaxValue)
			return GuildFailures.MaxRolesReached;

		var newPosition = maxPosition + 1;

		var roleResult = Role.Create(
			id: roleId,
			guildId: Id,
			name: name ?? string.Empty,
			color: color,
			permissions: permissions,
			position: newPosition,
			isDefault: false,
			isHoisted: isHoisted,
			isMentionable: isMentionable,
			now: now);
		if (roleResult.IsFailure)
			return roleResult.Error;

		_roles.Add(roleResult.Value);
		UpdatedAt = now;
		return roleResult.Value;
	}

	public Result<Role> UpdateRole(
		long roleId,
		string? name,
		string? color,
		long? permissions,
		int? position,
		bool? isHoisted,
		bool? isMentionable,
		DateTimeOffset now)
	{
		var role = _roles.FirstOrDefault(r => r.Id == roleId);
		if (role is null)
			return GuildFailures.RoleNotFound;

		var updateResult = role.UpdateSettings(name, color, permissions, position, isHoisted, isMentionable, now);
		if (updateResult.IsFailure)
			return updateResult.Error;

		UpdatedAt = now;
		return role;
	}

	public Result RemoveRole(long roleId, DateTimeOffset now)
	{
		var role = _roles.FirstOrDefault(r => r.Id == roleId);
		if (role is null)
			return GuildFailures.RoleNotFound;

		if (role.IsDefault)
			return GuildFailures.CannotDeleteDefaultRole;

		_roles.Remove(role);
		_memberRoles.RemoveAll(mr => mr.RoleId == roleId);
		UpdatedAt = now;
		return Result.Ok();
	}

	private static Result ValidateSettings(
		string? name,
		string? description,
		string? iconUrl,
		string? bannerUrl)
	{
		if (string.IsNullOrWhiteSpace(name))
			return GuildFailures.GuildNameRequired;

		if (name.Length > MaxNameLen)
			return GuildFailures.GuildNameTooLong;

		if (name.Any(char.IsControl))
			return GuildFailures.GuildNameInvalid;

		if (iconUrl is not null && iconUrl.Length > MaxUrlLen)
			return GuildFailures.GuildIconUrlTooLong;

		if (bannerUrl is not null && bannerUrl.Length > MaxUrlLen)
			return GuildFailures.GuildBannerUrlTooLong;

		_ = description; // no max length cap -> column is TEXT so it's not that important (yet* :P)

		return Result.Ok();
	}
}
