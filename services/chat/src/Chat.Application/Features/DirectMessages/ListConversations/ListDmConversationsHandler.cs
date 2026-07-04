using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.DirectMessages.ListConversations;

internal sealed class ListDmConversationsHandler(
	ICurrentUser currentUser,
	IMessageRepository repository)
	: IQueryHandler<ListDmConversationsQuery, Result<IReadOnlyList<DmConversationResponse>>>
{
	public async Task<Result<IReadOnlyList<DmConversationResponse>>> HandleAsync(
		ListDmConversationsQuery query,
		CancellationToken cancellationToken = default)
	{
		var conversations = await repository.GetConversationsAsync(currentUser.UserId, cancellationToken);
		var filtered = query.IncludeArchived ? conversations : conversations.Where(c => !c.IsArchived);

		return filtered
			.OrderByDescending(c => c.LastMessageAt)
			.Select(DmConversationResponse.From)
			.ToList();
	}
}
