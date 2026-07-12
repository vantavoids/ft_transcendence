using MassTransit;

namespace Chat.Infrastructure.Messaging.Contracts;

// cross-service events Chat *consumes* but does not publish. each carries a
// [MessageUrn] override so MassTransit binds the local consumer to the URN
// the publisher actually sends on the wire. without the override the URN
// would default to "Chat.Infrastructure.Messaging.Contracts:...", which
// would never match the publisher's "{PubAssembly}.Application.Contracts:..."
// and every message would land in the *_skipped queue

[MessageUrn("Guild.Application.Contracts:GuildMemberJoined")]
public sealed record GuildMemberJoined(long GuildId, string GuildName, long UserId);

[MessageUrn("Guild.Application.Contracts:GuildMemberLeft")]
public sealed record GuildMemberLeft(long GuildId, long UserId);

[MessageUrn("Guild.Application.Contracts:GuildDeleted")]
public sealed record GuildDeleted(long GuildId, IReadOnlyList<long> ChannelIds);

[MessageUrn("Guild.Application.Contracts:ChannelAccessRevoked")]
public sealed record ChannelAccessRevoked(long ChannelId, long UserId);

[MessageUrn("Guild.Application.Contracts:GuildChannelCreated")]
public sealed record GuildChannelCreated(long GuildId, ChannelPayload Channel, IReadOnlyList<long> EligibleUserIds);

[MessageUrn("Guild.Application.Contracts:GuildChannelUpdated")]
public sealed record GuildChannelUpdated(long GuildId, ChannelPayload Channel, IReadOnlyList<long> EligibleUserIds);

[MessageUrn("Guild.Application.Contracts:GuildChannelDeleted")]
public sealed record GuildChannelDeleted(long GuildId, long ChannelId, IReadOnlyList<long> EligibleUserIds);

// mirrors Guild's ChannelPayload wire shape (snake_case, snowflake ids as
// quoted strings). property names must match the publisher exactly.
public sealed record ChannelPayload(
	string Id,
	string GuildId,
	string? CategoryId,
	string Name,
	string? Topic,
	string Type,
	int Position,
	bool IsNsfw,
	int SlowmodeSeconds);

[MessageUrn("Guild.Application.Contracts:GuildCategoryCreated")]
public sealed record GuildCategoryCreated(long GuildId, CategoryPayload Category);

[MessageUrn("Guild.Application.Contracts:GuildCategoryUpdated")]
public sealed record GuildCategoryUpdated(long GuildId, CategoryPayload Category);

[MessageUrn("Guild.Application.Contracts:GuildCategoryDeleted")]
public sealed record GuildCategoryDeleted(long GuildId, long CategoryId);

public sealed record CategoryPayload(string Id, string GuildId, string Name, int Position);

[MessageUrn("Auth.Application.Contracts:UserLoggedOut")]
public sealed record UserLoggedOut(long UserId);

[MessageUrn("Auth.Application.Contracts:UserDeleted")]
public sealed record UserDeleted(long UserId);
