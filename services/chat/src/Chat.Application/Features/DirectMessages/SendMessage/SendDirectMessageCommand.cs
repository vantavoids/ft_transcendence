using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Application.Features.Messages.SendMessage;
using Chat.Domain.Results;

namespace Chat.Application.Features.DirectMessages.SendMessage;

public sealed record SendDirectMessageCommand(
	long RecipientId,
	string? Content,
	long? ReplyToId,
	IReadOnlyList<long> AttachmentIds,
	string? Nonce)
	: ICommand<Result<DirectMessageResponse>>, ISendMessageCommand;
