using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Attachments;
using Chat.Domain.Messages;

namespace Chat.UnitTests.Fakes;

public sealed class FakeMessageRepository : IMessageRepository
{
	private readonly List<Message> _saved = [];
	private readonly Dictionary<(long AuthorId, long ChannelId, string Nonce), long> _channelNonces = [];
	private readonly Dictionary<(long SenderId, long RecipientId, string Nonce), long> _dmNonces = [];
	private readonly Dictionary<(long, long), long> _conversations = [];
	private readonly Dictionary<long, IReadOnlyList<AttachmentMetadata>> _attachments = [];

	public IReadOnlyList<Message> Saved => _saved;

	/// <summary>attachments persisted alongside each message, keyed by message id</summary>
	public IReadOnlyDictionary<long, IReadOnlyList<AttachmentMetadata>> SavedAttachments => _attachments;

	private static (long, long) Pair(long a, long b) => a < b ? (a, b) : (b, a);

	public void WithConversation(long userA, long userB, long conversationId) =>
		_conversations[Pair(userA, userB)] = conversationId;

	public void Reset()
	{
		_saved.Clear();
		_channelNonces.Clear();
		_dmNonces.Clear();
		_conversations.Clear();
		_attachments.Clear();
		Updated.Clear();
		SoftDeleted.Clear();
	}

	public Task AddAsync(Message message, string? nonce, IReadOnlyList<AttachmentMetadata> attachments, CancellationToken ct)
	{
		_saved.Add(message);
		_attachments[message.Id] = attachments;

		if (nonce is null)
			return Task.CompletedTask;

		if (message.IsDirectMessage)
			_dmNonces[(message.AuthorId, message.RecipientId!.Value, nonce)] = message.Id;
		else
			_channelNonces[(message.AuthorId, message.ContainerId, nonce)] = message.Id;

		return Task.CompletedTask;
	}

	public Task<long?> FindChannelNonceAsync(long authorId, long channelId, string nonce, CancellationToken ct)
	{
		_channelNonces.TryGetValue((authorId, channelId, nonce), out var messageId);
		return Task.FromResult(messageId == 0 ? null : (long?)messageId);
	}

	public Task<long?> FindDmNonceAsync(long senderId, long recipientId, string nonce, CancellationToken ct)
	{
		_dmNonces.TryGetValue((senderId, recipientId, nonce), out var messageId);
		return Task.FromResult(messageId == 0 ? null : (long?)messageId);
	}

	public Task<Message?> GetByIdAsync(long messageId, CancellationToken ct)
	{
		var message = _saved.FirstOrDefault(m => m.Id == messageId);
		return Task.FromResult(message);
	}

	public Task UpdateContentAsync(Message message, CancellationToken ct)
	{
		Updated.Add(message);
		return Task.CompletedTask;
	}

	public Task SoftDeleteAsync(Message message, CancellationToken ct)
	{
		SoftDeleted.Add(message);
		return Task.CompletedTask;
	}

	public Task<IReadOnlyList<Message>> GetChannelMessagesAsync(long channelId, DateTimeOffset beforeTime, int limit, CancellationToken ct)
	{
		var result = _saved
			.Where(m => m.ContainerId == channelId && !m.IsDirectMessage && m.CreatedAt < beforeTime && !m.IsDeleted)
			.OrderByDescending(m => m.CreatedAt)
			.Take(limit)
			.ToList();
		return Task.FromResult<IReadOnlyList<Message>>(result);
	}

	public Task<IReadOnlyList<Message>> GetDirectMessagesAsync(long conversationId, DateTimeOffset beforeTime, int limit, CancellationToken ct)
	{
		var messages = _saved
			.Where(m => m.ContainerId == conversationId && m.IsDirectMessage && m.CreatedAt < beforeTime)
			.OrderByDescending(m => m.CreatedAt)
			.ThenByDescending(m => m.Id)
			.Take(limit)
			.ToList();

		return Task.FromResult<IReadOnlyList<Message>>(messages);
	}

	public Task<long?> FindConversationAsync(long senderId, long recipientId, CancellationToken ct) =>
		Task.FromResult(_conversations.TryGetValue(Pair(senderId, recipientId), out var id) ? id : (long?)null);

	public Task<long> GetOrCreateConversationAsync(long senderId, long recipientId, long candidateId, CancellationToken ct)
	{
		var pair = Pair(senderId, recipientId);
		if (!_conversations.TryGetValue(pair, out var id))
			_conversations[pair] = id = candidateId;

		return Task.FromResult(id);
	}

	public void Seed(Message message) => _saved.Add(message);

	public List<Message> Updated { get; } = [];
	public List<Message> SoftDeleted { get; } = [];
}
