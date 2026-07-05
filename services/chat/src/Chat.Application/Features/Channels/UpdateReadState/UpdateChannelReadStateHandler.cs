using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Features.Channels.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.Channels.UpdateReadState;

internal sealed class UpdateChannelReadStateHandler(
	ICurrentUser currentUser,
	IGuildClient guildClient,
	IMessageRepository messageRepository,
	IReadStateRepository readStates)
	: ICommandHandler<UpdateChannelReadStateCommand, Result<ChannelReadStateResponse>>
{
	private const long ReadMessages = 1L << 1;
	private const long Administrator = 1L << 8;

	public async Task<Result<ChannelReadStateResponse>> HandleAsync(
		UpdateChannelReadStateCommand command, CancellationToken cancellationToken = default)
	{
		var userId = currentUser.UserId;
		var membership = await guildClient.GetMembershipAsync(command.ChannelId, userId, cancellationToken);

		if (membership is null)
			return MessageFailures.ChannelNotFound;

		if (!membership.IsMember)
			return MessageFailures.NotAMember;

		if ((membership.Permissions & (ReadMessages | Administrator)) == 0)
			return MessageFailures.MissingReadPermission;

		var message = await messageRepository.GetByIdAsync(command.MessageId, cancellationToken);
		if (message is null || message.IsDeleted || message.IsDirectMessage || message.ContainerId != command.ChannelId)
			return MessageFailures.NotFound;

		// last_read_at is the target message's own timestamp, not the wall-clock
		// time of this call - CountChannelMessagesAfterAsync compares against it
		// to find messages newer than what the user actually read
		var state = await readStates.UpsertIfNewerAsync(
			userId, command.ChannelId, isDm: false, command.MessageId, message.CreatedAt, cancellationToken);

		var unreadCount = await readStates.CountChannelMessagesAfterAsync(
			command.ChannelId, state.LastReadAt!.Value, cancellationToken);

		return new ChannelReadStateResponse(
			command.ChannelId.ToString(), state.LastReadMessageId?.ToString(), unreadCount);
	}
}
