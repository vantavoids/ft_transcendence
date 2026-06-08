using Chat.Application.Abstractions.Messaging;
using Chat.Domain.Results;

namespace Chat.Application.Features.Messages.DeleteMessage;

public sealed record DeleteMessageCommand(long MessageId) : ICommand<Result>;
