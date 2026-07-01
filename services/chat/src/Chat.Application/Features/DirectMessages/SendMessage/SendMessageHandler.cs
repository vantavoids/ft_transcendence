using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Contracts;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Domain.Messages;
using Chat.Domain.Results;

namespace Chat.Application.Features.DirectMessages.SendMessage;

internal sealed class SendDirectMessageHandler(
	ICurrentUser currentUser,
	IDirectMessageRepository repository,
	ISnowflakeIdGenerator ids,
	IUserClient userClient,
	IClock clock,
	IEventBus eventBus,
	IConversationUnicast unicaster)
	: ICommandHandler<SendDirectMessageCommand, Result<DirectMessageResponse>>
{
	private const int MaxNonceLen = 64;

	public async Task<Result<DirectMessageResponse>> HandleAsync(
		SendDirectMessageCommand command,
		CancellationToken cancellationToken = default)
	{
		if (command.Nonce is { Length: > MaxNonceLen })
			return DirectMessageFailures.NonceTooLong;

		var userId = currentUser.UserId;
		var relationship = await userClient.GetUsersRelationship(userId, command.RecipientId, cancellationToken);
		if (relationship is null)
			return DirectMessageFailures.RecipientNotFound;

		if (relationship.Status is "blocked_by_them" or "blocked_by_me")
			return DirectMessageFailures.RecipientBlocked;

		if (command.Nonce is not null)
		{
			var existingId = await repository.FindNonceAsync(userId, command.RecipientId, command.Nonce, cancellationToken);
			if (existingId is not null)
			{
				var existing = await repository.GetByIdAsync(existingId.Value, cancellationToken);
				if (existing is not null)
					return DirectMessageResponse.From(existing, command.Nonce);
			}
		}

		var messageId = ids.NextId();
		var conversationId =
			await repository.GetOrCreateConversationAsync(userId, command.RecipientId, ids.NextId(), cancellationToken);

		if (command.ReplyToId is not null)
		{
			if (await repository.FindReplyExistsAsync(conversationId, command.ReplyToId.Value, cancellationToken) is null)
				return DirectMessageFailures.InvalidReplyTarget;
		}

		var messageResult = DirectMessage.Create(
			id: messageId,
			conversationId: conversationId,
			senderId: userId,
			recipientId: command.RecipientId,
			content: command.Content,
			replyToId: command.ReplyToId,
			now: clock.UtcNow);
		if (messageResult.IsFailure)
			return messageResult.Error;

		var message = messageResult.Value;
		
		// TODO: wire up attachment resolution; empty until then
		await repository.AddAsync(message, command.Nonce, [], cancellationToken);

		var response = DirectMessageResponse.From(message, command.Nonce);

		await eventBus.PublishAsync(
			new ChatDmSent(
				ConversationId: conversationId,
				MessageId: messageId,
				SenderId: userId,
				RecipientId: command.RecipientId,
				Content: message.Content!),
			cancellationToken);

		await unicaster.UnicastMessageAsync(command.RecipientId, response, cancellationToken);

		return response;
	}
}
