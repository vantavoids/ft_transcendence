using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Features.Channels.Common;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Application.Features.Messages.Common;
using Chat.Domain.Messages;
using Chat.Domain.Results;

namespace Chat.Application.Features.Messages.EditMessage;

internal sealed class EditMessageHandler(
	ICurrentUser currentUser,
	IMessageRepository repository,
	IClock clock,
	IChannelBroadcaster broadcaster,
	IConversationUnicast unicaster)
	: ICommandHandler<EditMessageCommand, Result<EditMessageResponse>>
{
	public async Task<Result<EditMessageResponse>> HandleAsync(
		EditMessageCommand command,
		CancellationToken cancellationToken = default)
	{
		var message = await repository.GetByIdAsync(command.MessageId, cancellationToken);
		if (message is null || message.IsDeleted)
			return MessageFailures.NotFound;

		if (message.AuthorId != currentUser.UserId)
			return MessageFailures.NotAuthor;

		var editResult = message.Edit(command.Content, clock.UtcNow);
		if (editResult.IsFailure)
			return editResult.Error;

		await repository.UpdateContentAsync(message, cancellationToken);
		await NotifyEditedAsync(message, cancellationToken);

		return EditMessageResponse.From(message);
	}

	private Task NotifyEditedAsync(Message message, CancellationToken ct) =>
		message.IsDirectMessage
			? unicaster.UnicastMessageEditedAsync(message.RecipientId!.Value, DirectMessageEditedEvent.From(message), ct)
			: broadcaster.BroadcastMessageEditedAsync(message.ContainerId, ChannelMessageEditedEvent.From(message), ct);
}
