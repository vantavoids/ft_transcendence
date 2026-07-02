using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.DirectMessages.ListMessages;

internal sealed class ListDirectMessagesHandler(
	ICurrentUser currentUser,
	IMessageRepository repository,
	IAttachmentRepository attachmentRepository,
	IClock clock)
	: IQueryHandler<ListDirectMessagesQuery, Result<IReadOnlyList<DirectMessageResponse>>>
{
	public async Task<Result<IReadOnlyList<DirectMessageResponse>>> HandleAsync(
		ListDirectMessagesQuery query,
		CancellationToken cancellationToken = default)
	{
		var userId = currentUser.UserId;

		if (query.RecipientId <= 0)
			return MessageFailures.InvalidRecipientId;

		if (query.RecipientId == userId)
			return MessageFailures.CannotMessageSelf;

		var limit = Math.Clamp(query.Limit, 1, 100);

		var conversationId = await repository.FindConversationAsync(
			userId,
			query.RecipientId,
			cancellationToken);

		if (conversationId is null)
			return Array.Empty<DirectMessageResponse>();

		var messages = await repository.GetDirectMessagesAsync(
			conversationId.Value,
			query.BeforeTime ?? clock.UtcNow,
			limit,
			cancellationToken);

		// hydrate the whole page's attachments in a single multi-message read rather
		// than one point read per message; the lookup yields an empty sequence for
		// any message that has none
		var messageIds = messages.Select(m => m.Id).ToList();
		var attachmentsByMessage = await attachmentRepository
			.GetMessagesAttachmentsAsync(conversationId.Value, isDm: true, messageIds, cancellationToken);

		return messages
			.Select(msg => DirectMessageResponse.From(msg, null, [.. attachmentsByMessage[msg.Id]]))
			.ToList();
	}
}
