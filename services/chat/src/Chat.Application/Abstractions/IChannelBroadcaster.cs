using Chat.Application.Features.Channels.Common;

namespace Chat.Application.Abstractions;

public interface IChannelBroadcaster
{
	Task BroadcastMessageAsync(long channelId, ChannelMessageResponse message, CancellationToken ct);
	Task BroadcastMessageEditedAsync(long channelId, ChannelMessageEditedEvent evt, CancellationToken ct);
	Task BroadcastMessageDeletedAsync(long channelId, long messageId, CancellationToken ct);
	Task BroadcastReactionAddedAsync(long channelId, ReactionAddedEvent evt, CancellationToken ct);
	Task BroadcastReactionRemovedAsync(long channelId, ReactionRemovedEvent evt, CancellationToken ct);
}
