using Chat.Domain.Messages;

namespace Chat.Application.Features.DirectMessages.Common;

public sealed record DirectMessageEditedEvent(string Id, string ConversationId, string? Content, DateTimeOffset EditedAt)
{
	public static DirectMessageEditedEvent From(Message m) => new(
		Id: m.Id.ToString(),
		ConversationId: m.ContainerId.ToString(),
		Content: m.Content,
		EditedAt: m.EditedAt!.Value);
}
