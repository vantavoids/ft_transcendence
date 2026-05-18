using Chat.Domain.Messages;

namespace Chat.Application.Abstractions.Persistence;

public interface IMessageRepository
{
	Task AddAsync(Message message, CancellationToken ct);
}
