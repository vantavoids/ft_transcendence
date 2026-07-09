using Chat.Domain.Messages;

namespace Chat.Application.Features.DirectMessages.Common;

public sealed record DirectMessageDeletedEvent(string MessageId, string ConversationId)
{
	public static DirectMessageDeletedEvent From(Message m) => new(
		MessageId: m.Id.ToString(),
		ConversationId: m.ContainerId.ToString());
}
