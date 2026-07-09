using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Domain.Messages;
using Chat.Domain.Results;

namespace Chat.Application.Features.Messages.DeleteMessage;

internal sealed class DeleteMessageHandler(
	ICurrentUser currentUser,
	IGuildClient guildClient,
	IMessageRepository repository,
	IChannelBroadcaster broadcaster,
	IConversationUnicast unicaster)
	: ICommandHandler<DeleteMessageCommand, Result>
{
	// permission bits mirror the Guild Service domain so this stays a pure
	// bitmask check; see services/guild/src/Guild.Domain/Guild/Permission.cs
	// for the source of truth on permission numbering
	private const long ManageMessages = 1L << 2;
	private const long Administrator = 1L << 8;

	public async Task<Result> HandleAsync(
		DeleteMessageCommand command,
		CancellationToken cancellationToken = default)
	{
		var message = await repository.GetByIdAsync(command.MessageId, cancellationToken);
		if (message is null || message.IsDeleted)
			return MessageFailures.NotFound;

		var authResult = await AuthorizeAsync(message, currentUser.UserId, cancellationToken);
		if (authResult.IsFailure)
			return authResult.Error;

		var deleteResult = message.Delete();
		if (deleteResult.IsFailure)
			return deleteResult.Error;

		await repository.SoftDeleteAsync(message, cancellationToken);
		await NotifyDeletedAsync(message, cancellationToken);

		return Result.Ok();
	}

	private async Task<Result> AuthorizeAsync(Message message, long userId, CancellationToken ct)
	{
		if (message.AuthorId == userId)
			return Result.Ok();

		// DMs have no moderation concept: only the author may delete
		if (message.IsDirectMessage)
			return MessageFailures.NotAuthor;

		var membership = await guildClient.GetMembershipAsync(message.ContainerId, userId, ct);
		if (membership is null || !membership.IsMember)
			return MessageFailures.NotFound;

		return (membership.Permissions & (ManageMessages | Administrator)) != 0
			? Result.Ok()
			: MessageFailures.MissingManagePermission;
	}

	private Task NotifyDeletedAsync(Message message, CancellationToken ct) =>
		message.IsDirectMessage
			? unicaster.UnicastMessageDeletedAsync(message.AuthorId, message.RecipientId!.Value, DirectMessageDeletedEvent.From(message), ct)
			: broadcaster.BroadcastMessageDeletedAsync(message.ContainerId, message.Id, ct);
}
