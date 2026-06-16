using Guild.Domain.Results;

namespace Guild.Domain.Guild;

public sealed class Role
{
	public const int MaxNameLen = 64;
	public const int MaxColorLen = 7;

	// EF Core constructor
	private Role() { }

	private Role(
		long id,
		long guildId,
		string name,
		string? color,
		long permissions,
		int position,
		bool isDefault,
		bool isHoisted,
		bool isMentionable,
		DateTimeOffset createdAt,
		DateTimeOffset updatedAt)
	{
		Id = id;
		GuildId = guildId;
		Name = name;
		Color = color;
		Permissions = permissions;
		Position = position;
		IsDefault = isDefault;
		IsHoisted = isHoisted;
		IsMentionable = isMentionable;
		CreatedAt = createdAt;
		UpdatedAt = updatedAt;
	}

	public long Id { get; private set; }
	public long GuildId { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string? Color { get; private set; }
	public long Permissions { get; private set; }
	public int Position { get; private set; }
	public bool IsDefault { get; private set; }
	public bool IsHoisted { get; private set; }
	public bool IsMentionable { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }

	public static Result<Role> Create(
		long id,
		long guildId,
		string name,
		string? color,
		long permissions,
		int position,
		bool isDefault,
		bool isHoisted,
		bool isMentionable,
		DateTimeOffset now)
	{
		if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLen || name.Any(char.IsControl))
			return GuildFailures.InvalidRoleName;

		if (color is not null && !IsValidHexColor(color))
			return GuildFailures.InvalidColor;

		return new Role(
			id: id,
			guildId: guildId,
			name: name,
			color: color,
			permissions: permissions,
			position: position,
			isDefault: isDefault,
			isHoisted: isHoisted,
			isMentionable: isMentionable,
			createdAt: now,
			updatedAt: now);
	}

	internal static Role CreateEveryone(long id, long guildId, DateTimeOffset now)
	{
		const long perms = (long)Permission.SendMessages
						   | (long)Permission.ReadMessages
						   | (long)Permission.CreateInvite; // 1 | 2 | 512 = 515

		return new Role(
			id: id,
			guildId: guildId,
			name: "@everyone",
			color: null,
			permissions: perms,
			position: 0,
			isDefault: true,
			isHoisted: false,
			isMentionable: false,
			createdAt: now,
			updatedAt: now);
	}

	internal static Role CreateAdministrator(long id, long guildId, DateTimeOffset now)
	{
		return new Role(
			id: id,
			guildId: guildId,
			name: "Administrator",
			color: null,
			permissions: (long)Permission.Administrator, // 256
			position: 1,
			isDefault: false,
			isHoisted: false,
			isMentionable: false,
			createdAt: now,
			updatedAt: now);
	}

	/// <summary>
	/// applies a PATCH-style diff. each parameter is "no change" when null
	/// (except color where empty string means "clear", matching the project's
	/// nullable-field PATCH convention used by UpdateGuild). default roles
	/// reject name/is_default edits but accept the rest (permissions, color,
	/// hoist, mentionability) so @everyone's baseline can still be tuned.
	/// position is intentionally NOT editable here: single-role position edits
	/// cannot atomically swap two roles without violating the unique
	/// (guild_id, position) invariant, so reordering goes through
	/// <see cref="Guild.ReorderRoles"/> instead (issue #213)
	/// </summary>
	public Result UpdateSettings(
		string? name,
		string? color,
		long? permissions,
		bool? isHoisted,
		bool? isMentionable,
		DateTimeOffset now)
	{
		if (IsDefault && name is not null)
			return GuildFailures.CannotEditDefaultRole;

		if (name is not null)
		{
			if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLen || name.Any(char.IsControl))
				return GuildFailures.InvalidRoleName;
			Name = name;
		}

		if (color is not null)
		{
			if (color.Length > 0 && !IsValidHexColor(color))
				return GuildFailures.InvalidColor;
			Color = color.Length == 0 ? null : color;
		}

		if (permissions is long newPerms)
		{
			if (newPerms < 0)
				return GuildFailures.RolePermissionsInvalid;
			Permissions = newPerms;
		}

		if (isHoisted is bool h) IsHoisted = h;
		if (isMentionable is bool m) IsMentionable = m;

		UpdatedAt = now;
		return Result.Ok();
	}

	/// <summary>
	/// sets the role's position. only the <see cref="Guild"/> aggregate calls
	/// this, from <see cref="Guild.ReorderRoles"/>, after it has validated that
	/// the full set of new positions is a collision-free permutation. kept off
	/// the public PATCH path on purpose (issue #213)
	/// </summary>
	internal void SetPosition(int position, DateTimeOffset now)
	{
		Position = position;
		UpdatedAt = now;
	}

	private static bool IsValidHexColor(string color)
	{
		if (color is not ['#', _, _, _, _, _, _])
			return false;

		for (var i = 1; i < 7; i++)
		{
			var c = color[i];
			var isHex = c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
			if (!isHex) return false;
		}
		return true;
	}
}
