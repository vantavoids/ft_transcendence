using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Messages;

namespace Chat.UnitTests.Fakes;

public sealed class FakeDirectMessageRepository : IDirectMessageRepository
{
	private readonly List<DirectMessage> _saved = [];
	private readonly Dictionary<(long SenderId, long RecipientId, string Nonce), long> _nonces = [];
	private readonly Dictionary<(long, long), long> _conversations = [];
	private readonly Dictionary<(long ConversationId, long ReplyToId), long> _replies = [];


	public IReadOnlyList<DirectMessage> Saved => _saved;

	private static (long, long) Pair(long a, long b) => a < b ? (a, b) : (b, a);

	public void WithConversation(long userA, long userB, long conversationId) =>
		_conversations[Pair(userA, userB)] = conversationId;

	public void Reset()
	{
		_saved.Clear();
		_nonces.Clear();
		_conversations.Clear();
		_replies.Clear();
	}

	public Task<long?> FindConversationAsync(long senderId, long recipientId, CancellationToken ct) =>
		Task.FromResult(_conversations.TryGetValue(Pair(senderId, recipientId), out var id) ? id : (long?)null);

	public Task<long?> FindReplyExistsAsync(long conversationId, long replyToId, CancellationToken ct) =>
		Task.FromResult(_replies.TryGetValue((conversationId, replyToId), out var id) ? id : (long?)null);

	public Task AddAsync(DirectMessage message, string? nonce, CancellationToken ct)
	{
		_saved.Add(message);
		_conversations[Pair(message.SenderId, message.RecipientId)] = message.ConversationId;
		if (nonce is not null)
			_nonces[(message.SenderId, message.RecipientId, nonce)] = message.Id;
		return Task.CompletedTask;
	}

	public Task<long?> FindNonceAsync(long senderId, long recipientId, string nonce, CancellationToken ct)
	{
		_nonces.TryGetValue((senderId, recipientId, nonce), out var messageId);
		return Task.FromResult(messageId == 0 ? null : (long?)messageId);
	}

	public Task<DirectMessage?> GetByIdAsync(long messageId, CancellationToken ct)
	{
		var message = _saved.FirstOrDefault(m => m.Id == messageId);
		return Task.FromResult(message);
	}

	public Task<IReadOnlyList<DirectMessage>> ListAsync(
			long conversationId,
			int limit,
			CancellationToken ct)
	{
		var messages = _saved
			.Where(m => m.ConversationId == conversationId)
			.OrderByDescending(m => m.CreatedAt)
			.ThenByDescending(m => m.Id)
			.Take(limit)
			.ToList();

		return Task.FromResult<IReadOnlyList<DirectMessage>>(messages);
	}

	public void WithReply(long conversationId, long replyToId) =>
		_replies[(conversationId, replyToId)] = replyToId;
}
