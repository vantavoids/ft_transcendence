using Chat.Domain.Attachments;
using Chat.Domain.Messages;
using Chat.Application.Features.Attachments.Common;

namespace Chat.Application.Features.Channels.Common;

/// <summary>
/// wire shape that mirrors the contract's <c>ReceiveMessage</c> event. snowflake
/// IDs are quoted strings so JS clients can hold them without precision loss.
/// reactions are an empty array on freshly-created messages
/// </summary>
public sealed record MessageResponse(
	string Id,
	string ChannelId,
	string AuthorId,
	string? Content,
	string? ReplyToId,
	DateTimeOffset? EditedAt,
	DateTimeOffset CreatedAt,
	IReadOnlyList<AttachmentResponse> Attachments,
	object[] Reactions,
	string? Nonce)
{
	public static MessageResponse From(
		Message m,
		string? nonce,
		IReadOnlyList<AttachmentMetadata>? attachments = null) => new(
		Id: m.Id.ToString(),
		ChannelId: m.ContainerId.ToString(),
		AuthorId: m.AuthorId.ToString(),
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
