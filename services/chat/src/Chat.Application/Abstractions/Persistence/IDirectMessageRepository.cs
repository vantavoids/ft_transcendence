using Chat.Domain.Messages;

namespace Chat.Application.Abstractions.Persistence;

public interface IDirectMessageRepository
{
	/// <summary>read-only lookup; does not create the conversation if it is missing</summary>
	Task<long?> FindConversationAsync(long userId, long partnerId, CancellationToken ct);

	/// <summary>
	/// resolves the conversation id for a (senderId, recipientId) pair, creating it
	/// atomically via a Cassandra lightweight transaction if this is the first
	/// message between the two users. <paramref name="candidateId"/> is used as the
	/// conversation id only if this call wins the race; otherwise the id agreed by
	/// the winner is returned.
	/// </summary>
	Task<long> GetOrCreateConversationAsync(long senderId, long recipientId, long candidateId, CancellationToken ct);

	Task<long?> FindReplyExistsAsync(long conversationId, long replyToId, CancellationToken ct);

	Task AddAsync(DirectMessage message, string? nonce, CancellationToken ct);

	Task<long?> FindNonceAsync(long senderId, long recipientId, string nonce, CancellationToken ct);

	Task<DirectMessage?> GetByIdAsync(long messageId, CancellationToken ct);
	Task<IReadOnlyList<DirectMessage>> ListAsync(long conversationId, int limit, CancellationToken ct);
}
