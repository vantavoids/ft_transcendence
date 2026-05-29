using Chat.Domain.Messages;

namespace Chat.Application.Features.Messages.Common;

/// <summary>
/// wire shape that mirrors the contract's <c>ReceiveMessage</c> event. snowflake
/// IDs are quoted strings so JS clients can hold them without precision loss.
/// attachments and reactions are empty arrays on freshly-created messages
/// </summary>
public sealed record MessageResponse(
	string Id,
	string ChannelId,
	string AuthorId,
	string? Content,
	string? ReplyToId,
	DateTimeOffset? EditedAt,
	DateTimeOffset CreatedAt,
	object[] Attachments,
	object[] Reactions,
	string? Nonce)
{
	public static MessageResponse From(Message m, string? nonce) => new(
		Id: m.Id.ToString(),
		ChannelId: m.ChannelId.ToString(),
		AuthorId: m.AuthorId.ToString(),
		Content: m.Content,
		ReplyToId: m.ReplyToId?.ToString(),
		EditedAt: m.EditedAt,
		CreatedAt: m.CreatedAt,
		Attachments: [],
		Reactions: [],
		Nonce: nonce);
}
