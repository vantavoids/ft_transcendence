using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.DirectMessages.ListMessages;

internal sealed class ListDirectMessagesHandler(
	ICurrentUser currentUser,
	IDirectMessageRepository repository,
	IClock clock)
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
			query.BeforeTime ?? clock.UtcNow,
			limit,
			cancellationToken);

		return messages
			.Select(msg => DirectMessageResponse.From(msg, null))
			.ToList();
	}
}
