namespace Chat.Application.Contracts;

public sealed record ChatMessageSent(long ChannelId, long GuildId, long AuthorId, long MessageId, string Content, long[] Mentions);
public sealed record CallIncoming(long CallId, long CallerId, long CalleeId, string CallType);
public sealed record UserOnline(long UserId);
public sealed record UserOffline(long UserId);

public sealed record GuildMemberJoined(long GuildId, long UserId);
public sealed record GuildMemberLeft(long GuildId, long UserId);
public sealed record UserLoggedOut(long UserId);
