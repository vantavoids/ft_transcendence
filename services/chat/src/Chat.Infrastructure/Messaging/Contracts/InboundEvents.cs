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

[MessageUrn("Auth.Application.Contracts:UserLoggedOut")]
public sealed record UserLoggedOut(long UserId);

[MessageUrn("Auth.Application.Contracts:UserDeleted")]
public sealed record UserDeleted(long UserId);
