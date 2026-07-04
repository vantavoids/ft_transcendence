using Chat.Domain.Attachments;
using Chat.Domain.Conversations;
using Chat.Domain.Messages;

namespace Chat.Application.Abstractions.Persistence;

/// <summary>
/// Persistence for both channel messages and direct messages. The two are
/// physically stored in separate tables (different partition keys, different
/// nonce-dedup tables), but share the same id space via <c>message_lookup</c>,
/// so a single <see cref="Message"/> shape and a single repository surface
/// avoid duplicating the send/edit/delete/list plumbing per message type.
/// </summary>
public interface IMessageRepository
{
	Task AddAsync(Message message, string? nonce, IReadOnlyList<AttachmentMetadata> attachments, CancellationToken ct);

	Task<long?> FindChannelNonceAsync(long authorId, long channelId, string nonce, CancellationToken ct);
	Task<long?> FindDmNonceAsync(long senderId, long recipientId, string nonce, CancellationToken ct);

	/// <summary>resolves via message_lookup; works for both channel messages and DMs</summary>
	Task<Message?> GetByIdAsync(long messageId, CancellationToken ct);

	Task UpdateContentAsync(Message message, CancellationToken ct);
	Task SoftDeleteAsync(Message message, CancellationToken ct);

	Task<IReadOnlyList<Message>> GetChannelMessagesAsync(long channelId, DateTimeOffset beforeTime, int limit, CancellationToken ct);
	Task<IReadOnlyList<Message>> GetDirectMessagesAsync(long conversationId, DateTimeOffset beforeTime, int limit, CancellationToken ct);

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

	Task DeleteConversationAsync(long userId, CancellationToken ct);

	/// <summary>every DM thread for this user, from their own user_conversations partition</summary>
	Task<IReadOnlyList<DmConversation>> GetConversationsAsync(long userId, CancellationToken ct);
}
