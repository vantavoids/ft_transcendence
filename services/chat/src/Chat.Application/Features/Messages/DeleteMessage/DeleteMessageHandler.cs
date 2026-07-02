using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Results;

namespace Chat.Application.Features.Messages.DeleteMessage;

internal sealed class DeleteMessageHandler(
	ICurrentUser currentUser,
	IGuildClient guildClient,
	IMessageRepository repository,
	IChannelBroadcaster broadcaster)
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
		if (message is null || message.IsDeleted || message.IsDirectMessage)
			return MessageFailures.NotFound;

		var userId = currentUser.UserId;

		if (message.AuthorId != userId)
		{
			var membership = await guildClient.GetMembershipAsync(message.ContainerId, userId, cancellationToken);
			if (membership is null || !membership.IsMember)
				return MessageFailures.NotFound;

			if ((membership.Permissions & (ManageMessages | Administrator)) == 0)
				return MessageFailures.MissingManagePermission;
		}

		var deleteResult = message.Delete();
		if (deleteResult.IsFailure)
			return deleteResult.Error;

		await repository.SoftDeleteAsync(message, cancellationToken);

		await broadcaster.BroadcastMessageDeletedAsync(
			message.ContainerId,
			message.Id,
			cancellationToken);

		return Result.Ok();
	}
}
