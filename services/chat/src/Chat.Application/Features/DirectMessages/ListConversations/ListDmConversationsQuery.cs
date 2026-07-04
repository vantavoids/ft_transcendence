using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.DirectMessages.ListConversations;

public sealed record ListDmConversationsQuery(bool IncludeArchived)
	: IQuery<Result<IReadOnlyList<DmConversationResponse>>>;
