using Chat.Application.Features.Messages.Common;

namespace Chat.Application.Abstractions;

public interface IChannelBroadcaster
{
	Task BroadcastMessageAsync(long channelId, MessageResponse message, CancellationToken ct);
}
