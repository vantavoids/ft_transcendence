namespace Chat.Application.Contracts;

// own-source events published by Chat. consumers in other services bind to
// these URNs via [MessageUrn("Chat.Application.Contracts:...")] on their side
public sealed record ChatMessageSent(long ChannelId, long GuildId, long AuthorId, long MessageId, string Content, long[] Mentions);
public sealed record CallIncoming(long CallId, long CallerId, long CalleeId, string CallType);
public sealed record UserOnline(long UserId);
public sealed record UserOffline(long UserId);

// cross-service inbound events live in Chat.Infrastructure.Messaging.Contracts
// so the [MessageUrn] attribute (which carries a MassTransit dependency) stays
// out of the Application layer. see InboundEvents.cs
