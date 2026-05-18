using Chat.Application.Abstractions;
using Chat.Application.Features.Messages.Common;

namespace Chat.UnitTests.Fakes;

public sealed class FakeChannelBroadcaster : IChannelBroadcaster
{
	private readonly List<(long ChannelId, MessageResponse Message)> _broadcasts = [];

	public IReadOnlyList<(long ChannelId, MessageResponse Message)> Broadcasts => _broadcasts;

	public void Reset() => _broadcasts.Clear();

	public Task BroadcastMessageAsync(long channelId, MessageResponse message, CancellationToken ct)
	{
		_broadcasts.Add((channelId, message));
		return Task.CompletedTask;
	}
}
