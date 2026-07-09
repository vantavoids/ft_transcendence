using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.DirectMessages.UpdateReadState;

internal sealed class UpdateDmReadStateHandler(
	ICurrentUser currentUser,
	IMessageRepository messageRepository,
	IReadStateRepository readStates,
	IUserBroadcaster broadcaster)
	: ICommandHandler<UpdateDmReadStateCommand, Result<DmReadStateResponse>>
{
	public async Task<Result<DmReadStateResponse>> HandleAsync(
		UpdateDmReadStateCommand command, CancellationToken cancellationToken = default)
	{
		// No "caller is not a participant" check: the conversation is resolved from
		// the (caller, partner) pair itself, so there is no path that could ever
		// reach a conversation the caller isn't part of.
		var userId = currentUser.UserId;

		var conversationId = await messageRepository.FindConversationAsync(userId, command.PartnerId, cancellationToken);
		if (conversationId is null)
			return MessageFailures.ConversationNotFound;

		var message = await messageRepository.GetByIdAsync(command.MessageId, cancellationToken);
		if (message is null || message.IsDeleted || !message.IsDirectMessage || message.ContainerId != conversationId.Value)
			return MessageFailures.NotFound;

		// see UpdateChannelReadStateHandler: last_read_at is the message's own
		// timestamp, not the wall-clock time of this call
		var state = await readStates.UpsertIfNewerAsync(
			userId, command.PartnerId, isDm: true, command.MessageId, message.CreatedAt, cancellationToken);

		// unlike the channel side, this is a whole-conversation reset, not a
		// precise "messages after cursor" recount: dm_unread_counts is a monotonic
		// counter (increment on send, reset on read)
		await readStates.ResetDmUnreadCountAsync(userId, command.PartnerId, cancellationToken);

		var response = new DmReadStateResponse(command.PartnerId.ToString(), state.LastReadMessageId?.ToString(), UnreadCount: 0);

		await broadcaster.BroadcastDmReadStateUpdatedAsync(userId, response, cancellationToken);

		return response;
	}
}
