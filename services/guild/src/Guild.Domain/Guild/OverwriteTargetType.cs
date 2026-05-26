namespace Guild.Domain.Guild;

/// <summary>
/// what a <see cref="ChannelPermissionOverwrite"/> applies to: a Role inside the
/// guild, or an individual member. persisted as "role"/"user" in the DB.
/// </summary>
public enum OverwriteTargetType
{
	Role = 0,
	Member = 1,
}
