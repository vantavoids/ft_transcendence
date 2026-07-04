namespace Chat.Domain.Conversations;

/// <summary>
/// storage projection of a row in user_conversations: one DM thread as seen
/// by a single participant (the partner's own row is independent)
/// </summary>
public sealed record DmConversation(
	long PartnerId,
	DateTimeOffset LastMessageAt,
	string? LastPreview,
	bool IsArchived);
