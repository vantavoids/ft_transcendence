using Guild.Domain.Guild;

namespace Guild.Application.Contracts;

public sealed record GuildMemberJoined(long GuildId, string GuildName, long UserId);
public sealed record GuildMemberLeft(long GuildId, long UserId);
public sealed record GuildInviteCreated(long GuildId, string GuildName, long InvitedByUserId, long? InvitedUserId);
public sealed record GuildDeleted(long GuildId);
public sealed record GuildOwnerTransferred(long GuildId, long OldOwnerId, long NewOwnerId);
public sealed record ChannelAccessRevoked(long ChannelId, long UserId);

// channel-lifecycle events consumed by Chat to push real-time structure updates.
// EligibleUserIds are the members whose effective permissions include
// ReadMessages for the channel: Chat targets only them so private (overwrite-
// restricted) channels never leak to members who cannot read them.
public sealed record GuildChannelCreated(long GuildId, ChannelPayload Channel, IReadOnlyList<long> EligibleUserIds);
public sealed record GuildChannelUpdated(long GuildId, ChannelPayload Channel, IReadOnlyList<long> EligibleUserIds);
public sealed record GuildChannelDeleted(long GuildId, long ChannelId, IReadOnlyList<long> EligibleUserIds);

public sealed record ChannelPayload(
	string Id,
	string GuildId,
	string? CategoryId,
	string Name,
	string? Topic,
	string Type,
	int Position,
	bool IsNsfw,
	int SlowmodeSeconds)
{
	public static ChannelPayload From(Channel c) => new(
		Id: c.Id.ToString(),
		GuildId: c.GuildId.ToString(),
		CategoryId: c.CategoryId?.ToString(),
		Name: c.Name,
		Topic: c.Topic,
		Type: c.Type.ToString().ToLowerInvariant(),
		Position: c.Position,
		IsNsfw: c.IsNsfw,
		SlowmodeSeconds: c.SlowmodeSeconds);
}
