using Chat.Application.Features.DirectMessages.Common;

namespace Chat.Application.Abstractions;

public interface IConversationUnicast
{
	Task UnicastMessageAsync(long recipientId, DirectMessageResponse message, CancellationToken ct);
	Task UnicastMessageEditedAsync(long recipientId, DirectMessageEditedEvent evt, CancellationToken ct);
	Task UnicastMessageDeletedAsync(long recipientId, DirectMessageDeletedEvent evt, CancellationToken ct);
}
