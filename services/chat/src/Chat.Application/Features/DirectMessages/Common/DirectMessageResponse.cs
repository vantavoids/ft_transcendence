using Chat.Domain.Messages;

namespace Chat.Application.Features.DirectMessages.Common;

/// <summary>
/// wire shape that mirrors the contract's <c>ReceiveMessage</c> event. snowflake
/// IDs are quoted strings so JS clients can hold them without precision loss.
/// attachments and reactions are empty arrays on freshly-created messages
/// </summary>
public sealed record DirectMessageResponse(
	string Id,
	string ConversationId,
	string SenderId,
    string RecipientId,
	string? Content,
	string? ReplyToId,
	DateTimeOffset? EditedAt,
	DateTimeOffset CreatedAt,
	object[] Attachments,
	object[] Reactions,
	string? Nonce)
{
	public static DirectMessageResponse From(DirectMessage m, string? nonce) => new(
		Id: m.Id.ToString(),
		ConversationId: m.ConversationId.ToString(),
		SenderId: m.SenderId.ToString(),
        RecipientId: m.RecipientId.ToString(),
		Content: m.Content,
		ReplyToId: m.ReplyToId?.ToString(),
		EditedAt: m.EditedAt,
		CreatedAt: m.CreatedAt,
		Attachments: [],
		Reactions: [],
		Nonce: nonce);
}
