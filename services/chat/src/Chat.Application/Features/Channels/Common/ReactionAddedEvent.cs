namespace Chat.Application.Features.Channels.Common;

public sealed record ReactionAddedEvent(string MessageId, string ChannelId, string UserId, string Emoji, long Count);
