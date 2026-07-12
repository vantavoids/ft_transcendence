using Guild.Domain.Guild;

namespace Guild.Application.Contracts;

public sealed record GuildMemberJoined(long GuildId, string GuildName, long UserId);
public sealed record GuildMemberLeft(long GuildId, long UserId);
public sealed record GuildInviteCreated(long GuildId, string GuildName, long InvitedByUserId, long? InvitedUserId);
// ChannelIds lists the guild's channels at deletion time so Chat (which cannot
// read Guild's DB) can purge each channel's message history.
public sealed record GuildDeleted(long GuildId, IReadOnlyList<long> ChannelIds);
public sealed record GuildOwnerTransferred(long GuildId, long OldOwnerId, long NewOwnerId);
public sealed record GuildUpdated(long GuildId, string Name, string? IconUrl);

// coalesced structure events: the frontend re-fetches the affected guild's roles
// / roster rather than patching bitmasks and hierarchy, so a single "something
// changed" signal per family is enough.
public sealed record GuildRolesChanged(long GuildId);
public sealed record GuildMemberUpdated(long GuildId, long UserId);
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

// category-lifecycle events consumed by Chat. categories carry no per-member
// read restriction, so Chat broadcasts them to the whole guild group.
public sealed record GuildCategoryCreated(long GuildId, CategoryPayload Category);
public sealed record GuildCategoryUpdated(long GuildId, CategoryPayload Category);
public sealed record GuildCategoryDeleted(long GuildId, long CategoryId);

public sealed record CategoryPayload(string Id, string GuildId, string Name, int Position)
{
	public static CategoryPayload From(ChannelCategory c) => new(
		Id: c.Id.ToString(),
		GuildId: c.GuildId.ToString(),
		Name: c.Name,
		Position: c.Position);
}
