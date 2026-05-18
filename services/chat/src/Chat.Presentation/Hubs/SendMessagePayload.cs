namespace Chat.Presentation.Hubs;

public sealed record SendMessagePayload(long ChannelId, string? Content, long? ReplyToId);
