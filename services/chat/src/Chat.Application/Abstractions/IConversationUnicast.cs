using Chat.Application.Features.DirectMessages.Common;

namespace Chat.Application.Abstractions;

public interface IConversationUnicast
{
	Task UnicastMessageAsync(DirectMessageResponse message, CancellationToken ct);
	Task UnicastMessageEditedAsync(long senderId, long recipientId, DirectMessageEditedEvent evt, CancellationToken ct);
	Task UnicastMessageDeletedAsync(long senderId, long recipientId, DirectMessageDeletedEvent evt, CancellationToken ct);
}
