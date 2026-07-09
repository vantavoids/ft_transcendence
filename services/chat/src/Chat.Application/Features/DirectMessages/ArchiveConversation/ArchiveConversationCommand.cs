using Chat.Application.Abstractions.Messaging;
using Chat.Domain.Results;

namespace Chat.Application.Features.DirectMessages.ArchiveConversation;

public sealed record ArchiveConversationCommand(long PartnerId) : ICommand<Result>;
