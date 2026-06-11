using Chat.Application.Abstractions;
using Chat.Application.Features.DirectMessages.Common;

namespace Chat.UnitTests.Fakes;

public sealed class FakeConversationUnicast : IConversationUnicast
{
	private readonly List<(long RecipientId, DirectMessageResponse Message)> _unicasts = [];

	public IReadOnlyList<(long RecipientId, DirectMessageResponse Message)> Unicasts => _unicasts;

	public void Reset() => _unicasts.Clear();
	public Task UnicastMessageAsync(long recipientId, DirectMessageResponse message, CancellationToken ct)
	{
		_unicasts.Add((recipientId, message));
		return Task.CompletedTask;
	}
}
