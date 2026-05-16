namespace Guild.Domain.Guild;

public sealed class MemberRole
{
	// EF Core constructor
	private MemberRole() { }

	private MemberRole(long guildId, long userId, long roleId, DateTimeOffset assignedAt)
	{
		GuildId = guildId;
		UserId = userId;
		RoleId = roleId;
		AssignedAt = assignedAt;
	}

	public long GuildId { get; private set; }
	public long UserId { get; private set; }
	public long RoleId { get; private set; }
	public DateTimeOffset AssignedAt { get; private set; }

	internal static MemberRole Create(long guildId, long userId, long roleId, DateTimeOffset now)
	{
		return new MemberRole(guildId, userId, roleId, now);
	}
}
