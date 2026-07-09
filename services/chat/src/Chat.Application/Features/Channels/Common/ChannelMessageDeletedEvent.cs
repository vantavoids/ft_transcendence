namespace Chat.Application.Features.Channels.Common;

public sealed record ChannelMessageDeletedEvent(string MessageId, string ChannelId);
