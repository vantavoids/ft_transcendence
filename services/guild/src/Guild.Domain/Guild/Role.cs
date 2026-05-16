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
		if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLen)
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
		long perms = (long)Permission.SendMessages
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

	private static bool IsValidHexColor(string color)
	{
		if (color.Length != 7 || color[0] != '#')
			return false;

		for (int i = 1; i < 7; i++)
		{
			char c = color[i];
			bool isHex = (c >= '0' && c <= '9')
					  || (c >= 'a' && c <= 'f')
					  || (c >= 'A' && c <= 'F');
			if (!isHex) return false;
		}
		return true;
	}
}
