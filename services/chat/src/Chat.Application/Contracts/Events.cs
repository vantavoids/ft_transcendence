namespace Chat.Application.Contracts;

public sealed record ChatMessageSent(Guid ChannelId, Guid GuildId, Guid AuthorId, string Content, Guid[] Mentions);
public sealed record CallIncoming(Guid CallId, Guid CallerId, Guid CalleeId, string CallType);
public sealed record UserOnline(Guid UserId);
public sealed record UserOffline(Guid UserId);

public sealed record GuildMemberJoined(Guid GuildId, Guid UserId);
public sealed record GuildMemberLeft(Guid GuildId, Guid UserId);
public sealed record UserLoggedOut(Guid UserId);
