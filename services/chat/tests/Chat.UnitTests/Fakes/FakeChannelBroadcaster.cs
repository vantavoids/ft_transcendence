using Chat.Application.Abstractions;
using Chat.Application.Features.Messages.Common;

namespace Chat.UnitTests.Fakes;

public sealed class FakeChannelBroadcaster : IChannelBroadcaster
{
	private readonly List<(long ChannelId, MessageResponse Message)> _broadcasts = [];
	private readonly List<(long ChannelId, MessageEditedEvent Evt)> _editedBroadcasts = [];
	private readonly List<(long ChannelId, long MessageId)> _deletedBroadcasts = [];

	public IReadOnlyList<(long ChannelId, MessageResponse Message)> Broadcasts => _broadcasts;
	public IReadOnlyList<(long ChannelId, MessageEditedEvent Evt)> EditedBroadcasts => _editedBroadcasts;
	public IReadOnlyList<(long ChannelId, long MessageId)> DeletedBroadcasts => _deletedBroadcasts;

	public void Reset()
	{
		_broadcasts.Clear();
		_editedBroadcasts.Clear();
		_deletedBroadcasts.Clear();
	}

	public Task BroadcastMessageAsync(long channelId, MessageResponse message, CancellationToken ct)
	{
		_broadcasts.Add((channelId, message));
		return Task.CompletedTask;
	}

	public Task BroadcastMessageEditedAsync(long channelId, MessageEditedEvent evt, CancellationToken ct)
	{
		_editedBroadcasts.Add((channelId, evt));
		return Task.CompletedTask;
	}

	public Task BroadcastMessageDeletedAsync(long channelId, long messageId, CancellationToken ct)
	{
		_deletedBroadcasts.Add((channelId, messageId));
		return Task.CompletedTask;
	}
}
