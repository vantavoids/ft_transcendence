using Chat.Domain.Attachments;
using Chat.Domain.Messages;

namespace Chat.Application.Abstractions.Persistence;

public interface IMessageRepository
{
	Task AddAsync(Message message, string? nonce, IReadOnlyList<AttachmentMetadata> attachments, CancellationToken ct);
	Task<long?> FindNonceAsync(long authorId, long channelId, string nonce, CancellationToken ct);
	Task<Message?> GetByIdAsync(long messageId, CancellationToken ct);
	Task UpdateContentAsync(Message message, CancellationToken ct);
	Task SoftDeleteAsync(Message message, CancellationToken ct);
	Task<IReadOnlyList<Message>> GetChannelMessagesAsync(long channelId, DateTimeOffset beforeTime, int limit, CancellationToken ct);
}
