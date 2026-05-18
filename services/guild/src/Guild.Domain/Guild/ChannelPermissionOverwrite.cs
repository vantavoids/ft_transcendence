using Guild.Domain.Results;

namespace Guild.Domain.Guild;

/// <summary>
/// per-channel allow/deny tweak applied on top of role-based permissions.
/// see <see cref="PermissionResolver.Resolve(Guild,long,Channel,System.Collections.Generic.IReadOnlyList{ChannelPermissionOverwrite})"/>
/// for the precedence order
/// </summary>
public sealed class ChannelPermissionOverwrite
{
	// EF Core constructor
	private ChannelPermissionOverwrite() { }

	private ChannelPermissionOverwrite(
		long id,
		long channelId,
		OverwriteTargetType targetType,
		long targetId,
		long allow,
		long deny,
		DateTimeOffset createdAt,
		DateTimeOffset updatedAt)
	{
		Id = id;
		ChannelId = channelId;
		TargetType = targetType;
		TargetId = targetId;
		Allow = allow;
		Deny = deny;
		CreatedAt = createdAt;
		UpdatedAt = updatedAt;
	}

	public long Id { get; private set; }
	public long ChannelId { get; private set; }
	public OverwriteTargetType TargetType { get; private set; }
	public long TargetId { get; private set; }
	public long Allow { get; private set; }
	public long Deny { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }

	public static Result<ChannelPermissionOverwrite> Create(
		long id,
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

		return new ChannelPermissionOverwrite(
			id: id,
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

		Allow = allow;
		Deny = deny;
		UpdatedAt = now;
		return Result.Ok();
	}
}
