using Chat.Domain.Attachments;
using Chat.Domain.Messages;
using Chat.Domain.Reactions;
using Chat.Application.Features.Attachments.Common;
using Chat.Application.Features.Messages.SendMessage;

namespace Chat.Application.Features.Channels.Common;

/// <summary>
/// wire shape that mirrors the contract's <c>ReceiveMessage</c> event. snowflake
/// IDs are quoted strings so JS clients can hold them without precision loss.
/// reactions are an empty array on freshly-created messages
/// </summary>
public sealed record ChannelMessageResponse(
	string Id,
	string ChannelId,
	string AuthorId,
	string? Content,
	string? ReplyToId,
	DateTimeOffset? EditedAt,
	DateTimeOffset CreatedAt,
	IReadOnlyList<AttachmentResponse> Attachments,
	IReadOnlyList<ReactionResponse> Reactions,
	string? Nonce) : IMessageWireResponse<ChannelMessageResponse>
{
	public static ChannelMessageResponse From(
		Message m,
		string? nonce,
		IReadOnlyList<AttachmentMetadata>? attachments = null,
		IReadOnlyList<ReactionSummary>? reactions = null) => new(
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
		Reactions: reactions is null or { Count: 0 }
			? []
			: reactions.Select(ReactionResponse.From).ToArray(),
		Nonce: nonce);
}
