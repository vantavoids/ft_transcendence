using Guild.Domain.Results;

namespace Guild.Domain.Guild;

public sealed class Guild
{
	public const int MaxNameLen = 100;
	public const int MaxUrlLen = 512;

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

		if (iconUrl is not null && iconUrl.Length > MaxUrlLen)
			return GuildFailures.GuildIconUrlTooLong;

		if (bannerUrl is not null && bannerUrl.Length > MaxUrlLen)
			return GuildFailures.GuildBannerUrlTooLong;

		_ = description; // no max length cap -> column is TEXT so it's not that important (yet* :P)

		return Result.Ok();
	}
}
