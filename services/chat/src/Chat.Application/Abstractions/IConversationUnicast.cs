using Chat.Application.Features.DirectMessages.Common;

namespace Chat.Application.Abstractions;

public interface IConversationUnicast
{
	Task UnicastMessageAsync(long recipientId, DirectMessageResponse message, CancellationToken ct);
}
