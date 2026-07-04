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
	// TODO: wire up unread_count when implement read-state
	public static DmConversationResponse From(DmConversation conversation) => new(
		PartnerId: conversation.PartnerId.ToString(),
		LastPreview: conversation.LastPreview,
		LastMessageAt: conversation.LastMessageAt,
		UnreadCount: 0,
		IsArchived: conversation.IsArchived);
}
