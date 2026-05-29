using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Contracts;
using Chat.Application.Features.Messages.Common;
using Chat.Domain.Messages;
using Chat.Domain.Results;

namespace Chat.Application.Features.Messages.SendMessage;

internal sealed class SendMessageHandler(
	ICurrentUser currentUser,
	IGuildClient guildClient,
	IMessageRepository repository,
	ISnowflakeIdGenerator ids,
	IClock clock,
	IEventBus eventBus,
	IChannelBroadcaster broadcaster)
	: ICommandHandler<SendMessageCommand, Result<MessageResponse>>
{
	// permission bits mirror the Guild Service domain so this stays a pure
	// bitmask check; see services/guild/src/Guild.Domain/Guild/Permission.cs
	// for the source of truth on permission numbering
	private const long SendMessages = 1L << 0;
	private const long Administrator = 1L << 8;
	private const int MaxNonceLen = 64;

	public async Task<Result<MessageResponse>> HandleAsync(
		SendMessageCommand command,
		CancellationToken cancellationToken = default)
	{
		if (command.Nonce is { Length: > MaxNonceLen })
			return MessageFailures.NonceTooLong;

		var userId = currentUser.UserId;

		var membership = await guildClient.GetMembershipAsync(command.ChannelId, userId, cancellationToken);
		if (membership is null)
			return MessageFailures.ChannelNotFound;

		if (!membership.IsMember)
			return MessageFailures.NotAMember;

		if ((membership.Permissions & (SendMessages | Administrator)) == 0)
			return MessageFailures.MissingSendPermission;

		var messageId = ids.NextId();

		var messageResult = Message.Create(
			id: messageId,
			channelId: command.ChannelId,
			authorId: userId,
			content: command.Content,
			replyToId: command.ReplyToId,
			now: clock.UtcNow);
		if (messageResult.IsFailure)
			return messageResult.Error;

		var message = messageResult.Value;
		await repository.AddAsync(message, cancellationToken);

		var response = MessageResponse.From(message, command.Nonce);

		await eventBus.PublishAsync(
			new ChatMessageSent(
				ChannelId: command.ChannelId,
				GuildId: membership.GuildId,
				AuthorId: userId,
				MessageId: messageId,
				Content: message.Content!,
				Mentions: []),
			cancellationToken);

		await broadcaster.BroadcastMessageAsync(command.ChannelId, response, cancellationToken);

		return response;
	}
}
