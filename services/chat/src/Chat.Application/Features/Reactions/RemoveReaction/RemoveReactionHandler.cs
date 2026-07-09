using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Features.Channels.Common;
using Chat.Domain.Reactions;
using Chat.Domain.Results;

namespace Chat.Application.Features.Reactions.RemoveReaction;

internal sealed class RemoveReactionHandler(
	ICurrentUser currentUser,
	IMessageRepository messageRepository,
	IReactionRepository reactionRepository,
	IGuildClient guildClient,
	IChannelBroadcaster broadcaster)
	: ICommandHandler<RemoveReactionCommand, Result<ReactionResponse>>
{
	private const long ReadMessages = 1L << 1;
	private const long Administrator = 1L << 8;

	public async Task<Result<ReactionResponse>> HandleAsync(RemoveReactionCommand command, CancellationToken cancellationToken = default)
	{
		var emojiResult = ReactionEmoji.Validate(command.Emoji);
		if (emojiResult.IsFailure)
			return emojiResult.Error;
		var emoji = emojiResult.Value;

		var message = await messageRepository.GetByIdAsync(command.MessageId, cancellationToken);
		if (message is null || message.IsDeleted)
			return MessageFailures.NotFound;

		if (message.IsDirectMessage)
			return ReactionFailures.IsDirectMessage;

		var userId = currentUser.UserId;
		var membership = await guildClient.GetMembershipAsync(message.ContainerId, userId, cancellationToken);
		if (membership is null || !membership.IsMember || (membership.Permissions & (ReadMessages | Administrator)) == 0)
			return MessageFailures.MissingReadPermission;

		var removed = await reactionRepository.RemoveAsync(message.ContainerId, message.Id, userId, emoji, cancellationToken);
		var count = await reactionRepository.GetCountAsync(message.ContainerId, message.Id, emoji, cancellationToken);

		if (removed)
			await broadcaster.BroadcastReactionRemovedAsync(
				message.ContainerId,
				new ReactionRemovedEvent(
					MessageId: message.Id.ToString(),
					ChannelId: message.ContainerId.ToString(),
					UserId: userId.ToString(),
					Emoji: emoji,
					Count: count),
				cancellationToken);

		return new ReactionResponse(emoji, count, MeReacted: false);
	}
}
