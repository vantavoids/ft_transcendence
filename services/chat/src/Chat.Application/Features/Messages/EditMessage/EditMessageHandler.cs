using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Features.Messages.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.Messages.EditMessage;

internal sealed class EditMessageHandler(
	ICurrentUser currentUser,
	IMessageRepository repository,
	IClock clock,
	IChannelBroadcaster broadcaster)
	: ICommandHandler<EditMessageCommand, Result<EditMessageResponse>>
{
	public async Task<Result<EditMessageResponse>> HandleAsync(
		EditMessageCommand command,
		CancellationToken cancellationToken = default)
	{
		var message = await repository.GetByIdAsync(command.MessageId, cancellationToken);
		if (message is null || message.IsDeleted || message.IsDirectMessage)
			return MessageFailures.NotFound;

		if (message.AuthorId != currentUser.UserId)
			return MessageFailures.NotAuthor;

		var editResult = message.Edit(command.Content, clock.UtcNow);
		if (editResult.IsFailure)
			return editResult.Error;

		await repository.UpdateContentAsync(message, cancellationToken);

		var evt = MessageEditedEvent.From(message);
		await broadcaster.BroadcastMessageEditedAsync(message.ContainerId, evt, cancellationToken);

		return EditMessageResponse.From(message);
	}
}
