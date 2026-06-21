using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.DirectMessages.ListMessages;

internal sealed class ListDirectMessagesHandler(
	ICurrentUser currentUser,
	IDirectMessageRepository repository)
	: IQueryHandler<ListDirectMessagesQuery, Result<IReadOnlyList<DirectMessageResponse>>>
{
	public async Task<Result<IReadOnlyList<DirectMessageResponse>>> HandleAsync(
		ListDirectMessagesQuery query,
		CancellationToken cancellationToken = default)
	{
		var userId = currentUser.UserId;

		if (query.RecipientId <= 0)
			return DirectMessageFailures.InvalidRecipientId;

		if (query.RecipientId == userId)
			return DirectMessageFailures.CannotMessageSelf;

		var limit = Math.Clamp(query.Limit, 1, 100);

		var conversationId = await repository.FindConversationAsync(
			userId,
			query.RecipientId,
			cancellationToken);

		if (conversationId is null)
			return Array.Empty<DirectMessageResponse>();

		var messages = await repository.ListAsync(
			conversationId.Value,
			limit,
			cancellationToken);

		return messages
			.Select(message => new DirectMessageResponse(
				Id: message.Id.ToString(),
				ConversationId: message.ConversationId.ToString(),
				SenderId: message.SenderId.ToString(),
				RecipientId: message.RecipientId.ToString(),
				Content: message.Content,
				ReplyToId: message.ReplyToId?.ToString(),
				EditedAt: message.EditedAt,
				CreatedAt: message.CreatedAt,
				Attachments: [],
				Reactions: [],
				Nonce: null))
			.ToList();
	}
}
