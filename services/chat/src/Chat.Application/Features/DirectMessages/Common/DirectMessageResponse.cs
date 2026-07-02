using Chat.Application.Features.Attachments.Common;
using Chat.Application.Features.Messages.SendMessage;
using Chat.Domain.Attachments;
using Chat.Domain.Messages;

namespace Chat.Application.Features.DirectMessages.Common;

/// <summary>
/// wire shape that mirrors the contract's <c>ReceiveMessage</c> event. snowflake
/// IDs are quoted strings so JS clients can hold them without precision loss.
/// reactions are an empty array on freshly-created messages
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
	IReadOnlyList<AttachmentResponse> Attachments,
	object[] Reactions,
	string? Nonce) : IMessageWireResponse<DirectMessageResponse>
{
	public static DirectMessageResponse From(
		Message m,
		string? nonce,
		IReadOnlyList<AttachmentMetadata>? attachments = null) => new(
		Id: m.Id.ToString(),
		ConversationId: m.ContainerId.ToString(),
		SenderId: m.AuthorId.ToString(),
		RecipientId: m.RecipientId!.Value.ToString(),
		Content: m.Content,
		ReplyToId: m.ReplyToId?.ToString(),
		EditedAt: m.EditedAt,
		CreatedAt: m.CreatedAt,
		Attachments: attachments is null or { Count: 0 }
			? []
			: attachments.Select(AttachmentResponse.From).ToArray(),
		Reactions: [],
		Nonce: nonce);
}
