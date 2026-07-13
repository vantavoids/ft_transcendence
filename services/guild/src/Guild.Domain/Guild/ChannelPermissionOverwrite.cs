using Guild.Domain.Results;

namespace Guild.Domain.Guild;

/// <summary>
/// per-channel allow/deny tweak applied on top of role-based permissions.
/// see <see cref="PermissionResolver.Resolve(Guild,long,Channel,IReadOnlyList{ChannelPermissionOverwrite})"/>
/// for the precedence order
/// </summary>
public sealed class ChannelPermissionOverwrite
{
	// EF Core constructor
	private ChannelPermissionOverwrite() { }

	private ChannelPermissionOverwrite(
		long id,
		long guildId,
		long channelId,
		OverwriteTargetType targetType,
		long targetId,
		long allow,
		long deny,
		DateTimeOffset createdAt,
		DateTimeOffset updatedAt)
	{
		Id = id;
		GuildId = guildId;
		ChannelId = channelId;
		TargetType = targetType;
		TargetId = targetId;
		Allow = allow;
		Deny = deny;
		CreatedAt = createdAt;
		UpdatedAt = updatedAt;
	}

	// bits meaningful as a per-channel allow/deny overwrite. guild-wide authority
	// (kick, ban, manage roles/guild, administrator, manage nicknames) is excluded
	// so an overwrite can never escalate a member past their guild-level
	// permissions -- critically, allowing ADMINISTRATOR here would short-circuit
	// every channel permission check for the target.
	public const long ChannelScopedMask =
		(long)(Permission.SendMessages | Permission.ReadMessages | Permission.ManageMessages
			| Permission.ManageChannels | Permission.CreateInvite | Permission.MentionEveryone);

	public long Id { get; private set; }
	public long GuildId { get; private set; }
	public long ChannelId { get; private set; }
	public OverwriteTargetType TargetType { get; private set; }
	public long TargetId { get; private set; }
	public long Allow { get; private set; }
	public long Deny { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }

	public static Result<ChannelPermissionOverwrite> Create(
		long id,
		long guildId,
		long channelId,
		OverwriteTargetType targetType,
		long targetId,
		long allow,
		long deny,
		DateTimeOffset now)
	{
		if (targetId <= 0 || !Enum.IsDefined(targetType))
			return GuildFailures.OverwriteInvalidTarget;

		if ((allow & deny) != 0L)
			return GuildFailures.OverwriteAllowDenyOverlap;

		if (((allow | deny) & ~ChannelScopedMask) != 0L)
			return GuildFailures.OverwriteUnsupportedPermission;

		return new ChannelPermissionOverwrite(
			id: id,
			guildId: guildId,
			channelId: channelId,
			targetType: targetType,
			targetId: targetId,
			allow: allow,
			deny: deny,
			createdAt: now,
			updatedAt: now);
	}

	public Result UpdatePermissions(long allow, long deny, DateTimeOffset now)
	{
		if ((allow & deny) != 0L)
			return GuildFailures.OverwriteAllowDenyOverlap;

		if (((allow | deny) & ~ChannelScopedMask) != 0L)
			return GuildFailures.OverwriteUnsupportedPermission;

		Allow = allow;
		Deny = deny;
		UpdatedAt = now;
		return Result.Ok();
	}
}
