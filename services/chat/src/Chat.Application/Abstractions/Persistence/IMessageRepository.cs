using Chat.Domain.Messages;

namespace Chat.Application.Abstractions.Persistence;

public interface IMessageRepository
{
	Task AddAsync(Message message, string? nonce, CancellationToken ct);
	Task<long?> FindNonceAsync(long authorId, long channelId, string nonce, CancellationToken ct);
	Task<Message?> GetByIdAsync(long messageId, CancellationToken ct);
}
