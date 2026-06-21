using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.DirectMessages.ListMessages;

public sealed record ListDirectMessagesQuery(
	long RecipientId,
	int Limit)
	: IQuery<Result<IReadOnlyList<DirectMessageResponse>>>;
