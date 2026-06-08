namespace Chat.Application.Features.Messages.Common;

public sealed record MessageDeletedEvent(string MessageId, string ChannelId);
