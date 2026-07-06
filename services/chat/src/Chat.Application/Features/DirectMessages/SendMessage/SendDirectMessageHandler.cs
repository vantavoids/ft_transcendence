using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Contracts;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Application.Features.Messages.SendMessage;
using Chat.Domain.Messages;
using Chat.Domain.Results;

namespace Chat.Application.Features.DirectMessages.SendMessage;

internal sealed class SendDirectMessageHandler(
	ICurrentUser currentUser,
	IMessageRepository repository,
	IAttachmentRepository attachmentRepository,
	ISnowflakeIdGenerator ids,
	IClock clock,
	IUserClient userClient,
	IEventBus eventBus,
	IConversationUnicast unicaster)
	: SendMessageHandlerBase<SendDirectMessageCommand, DirectMessageResponse, NoContext>(currentUser, repository, attachmentRepository, ids, clock)
{
	protected override async Task<Result<NoContext>> PrecheckAsync(SendDirectMessageCommand command, CancellationToken ct)
	{
		var relationship = await userClient.GetUsersRelationship(AuthorId, command.RecipientId, ct);
		if (relationship is null)
			return MessageFailures.RecipientNotFound;

		return relationship.Status is "blocked_by_them" or "blocked_by_me"
			? MessageFailures.RecipientBlocked
			: new NoContext();
	}

	protected override Task<long?> FindNonceAsync(SendDirectMessageCommand command, string nonce, CancellationToken ct) =>
		Repository.FindDmNonceAsync(AuthorId, command.RecipientId, nonce, ct);

	protected override Task<long?> FindExistingContainerIdAsync(SendDirectMessageCommand command, CancellationToken ct) =>
		Repository.FindConversationAsync(AuthorId, command.RecipientId, ct);

	protected override Task<long> ResolveContainerIdAsync(SendDirectMessageCommand command, CancellationToken ct) =>
		Repository.GetOrCreateConversationAsync(AuthorId, command.RecipientId, Ids.NextId(), ct);

	protected override Result<Message> CreateMessage(
		SendDirectMessageCommand command, long containerId, long messageId, bool hasAttachments, DateTimeOffset now) =>
		Message.CreateForDirectMessage(
			id: messageId,
			conversationId: containerId,
			senderId: AuthorId,
			recipientId: command.RecipientId,
			content: command.Content,
			replyToId: command.ReplyToId,
			now: now,
			hasAttachments: hasAttachments);

	protected override async Task PublishAndNotifyAsync(
		SendDirectMessageCommand command, NoContext context, Message message, DirectMessageResponse response, CancellationToken ct)
	{
		await eventBus.PublishAsync(
			new ChatDmSent(
				ConversationId: message.ContainerId,
				MessageId: message.Id,
				SenderId: AuthorId,
				RecipientId: command.RecipientId,
				Content: message.Content ?? string.Empty),
			ct);

		await unicaster.UnicastMessageAsync(response, ct);
	}
}
