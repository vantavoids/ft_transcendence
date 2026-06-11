using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.DirectMessages.SendMessage;

public sealed record SendDirectMessageCommand(long RecipientId, string? Content, long? ReplyToId, string? Nonce)
	: ICommand<Result<DirectMessageResponse>>;
