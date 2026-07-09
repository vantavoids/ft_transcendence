using Chat.Application.Abstractions;
using Chat.Application.Features.Channels.Common;

namespace Chat.UnitTests.Fakes;

public sealed class FakeChannelBroadcaster : IChannelBroadcaster
{
	private readonly List<(long ChannelId, ChannelMessageResponse Message)> _broadcasts = [];
	private readonly List<(long ChannelId, ChannelMessageEditedEvent Evt)> _editedBroadcasts = [];
	private readonly List<(long ChannelId, long MessageId)> _deletedBroadcasts = [];
	private readonly List<(long ChannelId, ReactionAddedEvent Evt)> _reactionAddedBroadcasts = [];
	private readonly List<(long ChannelId, ReactionRemovedEvent Evt)> _reactionRemovedBroadcasts = [];

	public IReadOnlyList<(long ChannelId, ChannelMessageResponse Message)> Broadcasts => _broadcasts;
	public IReadOnlyList<(long ChannelId, ChannelMessageEditedEvent Evt)> EditedBroadcasts => _editedBroadcasts;
	public IReadOnlyList<(long ChannelId, long MessageId)> DeletedBroadcasts => _deletedBroadcasts;
	public IReadOnlyList<(long ChannelId, ReactionAddedEvent Evt)> ReactionAddedBroadcasts => _reactionAddedBroadcasts;
	public IReadOnlyList<(long ChannelId, ReactionRemovedEvent Evt)> ReactionRemovedBroadcasts => _reactionRemovedBroadcasts;

	public void Reset()
	{
		_broadcasts.Clear();
		_editedBroadcasts.Clear();
		_deletedBroadcasts.Clear();
		_reactionAddedBroadcasts.Clear();
		_reactionRemovedBroadcasts.Clear();
	}

	public Task BroadcastMessageAsync(long channelId, ChannelMessageResponse message, CancellationToken ct)
	{
		_broadcasts.Add((channelId, message));
		return Task.CompletedTask;
	}

	public Task BroadcastMessageEditedAsync(long channelId, ChannelMessageEditedEvent evt, CancellationToken ct)
	{
		_editedBroadcasts.Add((channelId, evt));
		return Task.CompletedTask;
	}

	public Task BroadcastMessageDeletedAsync(long channelId, long messageId, CancellationToken ct)
	{
		_deletedBroadcasts.Add((channelId, messageId));
		return Task.CompletedTask;
	}

	public Task BroadcastReactionAddedAsync(long channelId, ReactionAddedEvent evt, CancellationToken ct)
	{
		_reactionAddedBroadcasts.Add((channelId, evt));
		return Task.CompletedTask;
	}

	public Task BroadcastReactionRemovedAsync(long channelId, ReactionRemovedEvent evt, CancellationToken ct)
	{
		_reactionRemovedBroadcasts.Add((channelId, evt));
		return Task.CompletedTask;
	}
}
