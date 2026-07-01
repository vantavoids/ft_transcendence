using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Attachments;
using Chat.Domain.Messages;

namespace Chat.UnitTests.Fakes;

public sealed class FakeMessageRepository : IMessageRepository
{
	private readonly List<Message> _saved = [];
	private readonly Dictionary<(long AuthorId, long ChannelId, string Nonce), long> _nonces = [];
	private readonly Dictionary<long, IReadOnlyList<AttachmentMetadata>> _attachments = [];

	public IReadOnlyList<Message> Saved => _saved;

	/// <summary>attachments persisted alongside each message, keyed by message id</summary>
	public IReadOnlyDictionary<long, IReadOnlyList<AttachmentMetadata>> SavedAttachments => _attachments;

	public void Reset()
	{
		_saved.Clear();
		_nonces.Clear();
		_attachments.Clear();
	}

	public Task AddAsync(Message message, string? nonce, IReadOnlyList<AttachmentMetadata> attachments, CancellationToken ct)
	{
		_saved.Add(message);
		_attachments[message.Id] = attachments;
		if (nonce is not null)
			_nonces[(message.AuthorId, message.ChannelId, nonce)] = message.Id;
		return Task.CompletedTask;
	}

	public Task<long?> FindNonceAsync(long authorId, long channelId, string nonce, CancellationToken ct)
	{
		_nonces.TryGetValue((authorId, channelId, nonce), out var messageId);
		return Task.FromResult(messageId == 0 ? null : (long?)messageId);
	}

	public Task<Message?> GetByIdAsync(long messageId, CancellationToken ct)
	{
		var message = _saved.FirstOrDefault(m => m.Id == messageId);
		return Task.FromResult(message);
	}

	public Task<long?> FindReplyExistsAsync(long channelId, long replyToId, CancellationToken ct)
	{
		var message = _saved.FirstOrDefault(m => m.Id == replyToId);
		var found = message is not null && message.ChannelId == channelId && !message.IsDeleted;
		return Task.FromResult(found ? (long?)replyToId : null);
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
			.Where(m => m.ChannelId == channelId && m.CreatedAt < beforeTime && !m.IsDeleted)
			.OrderByDescending(m => m.CreatedAt)
			.Take(limit)
			.ToList();
		return Task.FromResult<IReadOnlyList<Message>>(result);
	}

	public void Seed(Message message) => _saved.Add(message);

	public List<Message> Updated { get; } = [];
	public List<Message> SoftDeleted { get; } = [];
}
