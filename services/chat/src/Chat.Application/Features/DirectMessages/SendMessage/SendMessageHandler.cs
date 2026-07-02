using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Contracts;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Domain.Attachments;
using Chat.Domain.Messages;
using Chat.Domain.Results;

namespace Chat.Application.Features.DirectMessages.SendMessage;

internal sealed class SendDirectMessageHandler(
	ICurrentUser currentUser,
	IMessageRepository repository,
	IAttachmentRepository attachmentRepository,
	ISnowflakeIdGenerator ids,
	IUserClient userClient,
	IClock clock,
	IEventBus eventBus,
	IConversationUnicast unicaster)
	: ICommandHandler<SendDirectMessageCommand, Result<DirectMessageResponse>>
{
	private const int MaxNonceLen = 64;
	private const int MaxAttachments = 10;

	public async Task<Result<DirectMessageResponse>> HandleAsync(
		SendDirectMessageCommand command,
		CancellationToken cancellationToken = default)
	{
		if (command.Nonce is { Length: > MaxNonceLen })
			return MessageFailures.NonceTooLong;

		var userId = currentUser.UserId;
		var relationship = await userClient.GetUsersRelationship(userId, command.RecipientId, cancellationToken);
		if (relationship is null)
			return MessageFailures.RecipientNotFound;

		if (relationship.Status is "blocked_by_them" or "blocked_by_me")
			return MessageFailures.RecipientBlocked;

		if (command.Nonce is not null)
		{
			var existingId = await repository.FindDmNonceAsync(userId, command.RecipientId, command.Nonce, cancellationToken);
			if (existingId is not null)
			{
				var existing = await repository.GetByIdAsync(existingId.Value, cancellationToken);
				if (existing is not null)
				{
					var existingAttachments = await attachmentRepository
						.GetMessageAttachmentsAsync(existing.ContainerId, isDm: true, existing.Id, cancellationToken);
					return DirectMessageResponse.From(existing, command.Nonce, existingAttachments);
				}
			}
		}

		var attachmentsResult = await ResolveAttachmentsAsync(command.AttachmentIds, userId, cancellationToken);
		if (attachmentsResult.IsFailure)
			return attachmentsResult.Error;
		var attachments = attachmentsResult.Value;

		var messageId = ids.NextId();
		var conversationId =
			await repository.GetOrCreateConversationAsync(userId, command.RecipientId, ids.NextId(), cancellationToken);

		if (command.ReplyToId is not null)
		{
			var replyTarget = await repository.GetByIdAsync(command.ReplyToId.Value, cancellationToken);
			if (replyTarget is null || replyTarget.IsDeleted || replyTarget.ContainerId != conversationId)
				return MessageFailures.InvalidReplyTarget;
		}

		var messageResult = Message.CreateForDirectMessage(
			id: messageId,
			conversationId: conversationId,
			senderId: userId,
			recipientId: command.RecipientId,
			content: command.Content,
			replyToId: command.ReplyToId,
			now: clock.UtcNow,
			hasAttachments: attachments.Count > 0);
		if (messageResult.IsFailure)
			return messageResult.Error;

		var message = messageResult.Value;
		await repository.AddAsync(message, command.Nonce, attachments, cancellationToken);

		var response = DirectMessageResponse.From(message, command.Nonce, attachments);

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

	private async Task<Result<IReadOnlyList<AttachmentMetadata>>> ResolveAttachmentsAsync(
		IReadOnlyList<long> attachmentIds,
		long userId,
		CancellationToken ct)
	{
		if (attachmentIds.Count == 0)
			return Array.Empty<AttachmentMetadata>();

		if (attachmentIds.Count > MaxAttachments)
			return AttachmentFailures.TooMany;

		var resolved = new List<AttachmentMetadata>(attachmentIds.Count);
		foreach (var attachmentId in attachmentIds.Distinct())
		{
			// each referenced draft must exist, be owned by the caller, and not yet
			// belong to another message; an expired draft is simply gone (table TTL)
			var draft = await attachmentRepository.GetDraftAsync(attachmentId, ct);
			if (draft is null || draft.UploaderId != userId)
				return AttachmentFailures.InvalidReference;

			if (await attachmentRepository.IsAttachedAsync(attachmentId, ct))
				return AttachmentFailures.InvalidReference;

			resolved.Add(draft.ToMetadata());
		}

		return resolved;
	}
}
