using Chat.Application.Abstractions;
using Chat.Application.Features.DirectMessages.Common;

namespace Chat.UnitTests.Fakes;

public sealed class FakeConversationUnicast : IConversationUnicast
{
	private readonly List<DirectMessageResponse> _unicasts = [];
	private readonly List<(long SenderId, long RecipientId, DirectMessageEditedEvent Evt)> _editedUnicasts = [];
	private readonly List<(long SenderId, long RecipientId, DirectMessageDeletedEvent Evt)> _deletedUnicasts = [];

	public IReadOnlyList<DirectMessageResponse> Unicasts => _unicasts;
	public IReadOnlyList<(long SenderId, long RecipientId, DirectMessageEditedEvent Evt)> EditedUnicasts => _editedUnicasts;
	public IReadOnlyList<(long SenderId, long RecipientId, DirectMessageDeletedEvent Evt)> DeletedUnicasts => _deletedUnicasts;

	public void Reset()
	{
		_unicasts.Clear();
		_editedUnicasts.Clear();
		_deletedUnicasts.Clear();
	}

	public Task UnicastMessageAsync(DirectMessageResponse message, CancellationToken ct)
	{
		_unicasts.Add(message);
		return Task.CompletedTask;
	}

	public Task UnicastMessageEditedAsync(long senderId, long recipientId, DirectMessageEditedEvent evt, CancellationToken ct)
	{
		_editedUnicasts.Add((senderId, recipientId, evt));
		return Task.CompletedTask;
	}

	public Task UnicastMessageDeletedAsync(long senderId, long recipientId, DirectMessageDeletedEvent evt, CancellationToken ct)
	{
		_deletedUnicasts.Add((senderId, recipientId, evt));
		return Task.CompletedTask;
	}
}
