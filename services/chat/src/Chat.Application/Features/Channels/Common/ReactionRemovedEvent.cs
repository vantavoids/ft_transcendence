namespace Chat.Application.Features.Channels.Common;

public sealed record ReactionRemovedEvent(string MessageId, string ChannelId, string UserId, string Emoji, long Count);
