using Chat.Domain.Conversations;

namespace Chat.Application.Features.DirectMessages.Common;

/// <summary>wire shape for a GET /dms sidebar row</summary>
public sealed record DmConversationResponse(
	string PartnerId,
	string? LastPreview,
	DateTimeOffset LastMessageAt,
	int UnreadCount,
	bool IsArchived)
{
	public static DmConversationResponse From(DmConversation conversation, int unreadCount) => new(
		PartnerId: conversation.PartnerId.ToString(),
		LastPreview: conversation.LastPreview,
		LastMessageAt: conversation.LastMessageAt,
		UnreadCount: unreadCount,
		IsArchived: conversation.IsArchived);
}
