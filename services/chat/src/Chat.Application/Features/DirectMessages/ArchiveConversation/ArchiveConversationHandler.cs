using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Results;

namespace Chat.Application.Features.DirectMessages.ArchiveConversation;

internal sealed class ArchiveConversationHandler(
	ICurrentUser currentUser,
	IMessageRepository repository)
	: ICommandHandler<ArchiveConversationCommand, Result>
{
	public async Task<Result> HandleAsync(ArchiveConversationCommand command, CancellationToken cancellationToken = default)
	{
		var conversationId = await repository.FindConversationAsync(currentUser.UserId, command.PartnerId, cancellationToken);
		if (conversationId is null)
			return MessageFailures.ConversationNotFound;

		await repository.ArchiveConversationAsync(currentUser.UserId, command.PartnerId, cancellationToken);

		return Result.Ok();
	}
}
