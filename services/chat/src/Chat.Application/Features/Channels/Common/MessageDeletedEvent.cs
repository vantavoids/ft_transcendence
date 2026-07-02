namespace Chat.Application.Features.Channels.Common;

public sealed record MessageDeletedEvent(string MessageId, string ChannelId);
