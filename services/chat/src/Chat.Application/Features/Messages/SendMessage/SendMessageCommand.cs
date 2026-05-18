using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Messages.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.Messages.SendMessage;

public sealed record SendMessageCommand(long ChannelId, string? Content, long? ReplyToId)
	: ICommand<Result<MessageResponse>>;
