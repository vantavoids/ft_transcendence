using Chat.Domain.Messages;

namespace Chat.Application.Abstractions.Persistence;

public interface IDirectMessageRepository
{
	Task<long?> FindConversationAsync(long senderId, long recipientId, CancellationToken ct);
	Task<long?> FindReplyExistsAsync(long conversationId, long replyToId, CancellationToken ct);
	Task AddAsync(DirectMessage message, string? nonce, CancellationToken ct);
	Task<long?> FindNonceAsync(long senderId, long recipientId, string nonce, CancellationToken ct);
	Task<DirectMessage?> GetByIdAsync(long messageId, CancellationToken ct);
}
