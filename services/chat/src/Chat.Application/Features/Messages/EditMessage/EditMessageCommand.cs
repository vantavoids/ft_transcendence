using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Messages.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.Messages.EditMessage;

public sealed record EditMessageCommand(long MessageId, string? Content)
	: ICommand<Result<EditMessageResponse>>;
