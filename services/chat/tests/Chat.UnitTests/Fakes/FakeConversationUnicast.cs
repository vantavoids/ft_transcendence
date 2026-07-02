using Chat.Application.Abstractions;
using Chat.Application.Features.DirectMessages.Common;

namespace Chat.UnitTests.Fakes;

public sealed class FakeConversationUnicast : IConversationUnicast
{
	private readonly List<(long RecipientId, DirectMessageResponse Message)> _unicasts = [];
	private readonly List<(long RecipientId, DirectMessageEditedEvent Evt)> _editedUnicasts = [];
	private readonly List<(long RecipientId, DirectMessageDeletedEvent Evt)> _deletedUnicasts = [];

	public IReadOnlyList<(long RecipientId, DirectMessageResponse Message)> Unicasts => _unicasts;
	public IReadOnlyList<(long RecipientId, DirectMessageEditedEvent Evt)> EditedUnicasts => _editedUnicasts;
	public IReadOnlyList<(long RecipientId, DirectMessageDeletedEvent Evt)> DeletedUnicasts => _deletedUnicasts;

	public void Reset()
	{
		_unicasts.Clear();
		_editedUnicasts.Clear();
		_deletedUnicasts.Clear();
	}

	public Task UnicastMessageAsync(long recipientId, DirectMessageResponse message, CancellationToken ct)
	{
		_unicasts.Add((recipientId, message));
		return Task.CompletedTask;
	}

	public Task UnicastMessageEditedAsync(long recipientId, DirectMessageEditedEvent evt, CancellationToken ct)
	{
		_editedUnicasts.Add((recipientId, evt));
		return Task.CompletedTask;
	}

	public Task UnicastMessageDeletedAsync(long recipientId, DirectMessageDeletedEvent evt, CancellationToken ct)
	{
		_deletedUnicasts.Add((recipientId, evt));
		return Task.CompletedTask;
	}
}
