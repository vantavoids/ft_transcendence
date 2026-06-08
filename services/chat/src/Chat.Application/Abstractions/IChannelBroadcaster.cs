using Chat.Application.Features.Messages.Common;

namespace Chat.Application.Abstractions;

public interface IChannelBroadcaster
{
	Task BroadcastMessageAsync(long channelId, MessageResponse message, CancellationToken ct);
	Task BroadcastMessageEditedAsync(long channelId, MessageEditedEvent evt, CancellationToken ct);
	Task BroadcastMessageDeletedAsync(long channelId, long messageId, CancellationToken ct);
}
