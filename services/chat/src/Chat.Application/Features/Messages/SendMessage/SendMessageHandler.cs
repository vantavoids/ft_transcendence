using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Contracts;
using Chat.Application.Features.Messages.Common;
using Chat.Domain.Attachments;
using Chat.Domain.Messages;
using Chat.Domain.Results;

namespace Chat.Application.Features.Messages.SendMessage;

internal sealed class SendMessageHandler(
	ICurrentUser currentUser,
	IGuildClient guildClient,
	IMessageRepository repository,
	IAttachmentRepository attachmentRepository,
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
	private const int MaxAttachments = 10;

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

		// nonce dedup runs before draft validation: a retried send re-references
		// drafts that the first call already consumed, so it must short-circuit here
		if (command.Nonce is not null)
		{
			var existingId = await repository.FindNonceAsync(userId, command.ChannelId, command.Nonce, cancellationToken);
			if (existingId is not null)
			{
				var existing = await repository.GetByIdAsync(existingId.Value, cancellationToken);
				if (existing is not null)
				{
					var existingAttachments = await attachmentRepository
						.GetChannelMessageAttachmentsAsync(existing.ChannelId, existing.Id, cancellationToken);
					return MessageResponse.From(existing, command.Nonce, existingAttachments);
				}
			}
		}

		var attachmentsResult = await ResolveAttachmentsAsync(command.AttachmentIds, userId, cancellationToken);
		if (attachmentsResult.IsFailure)
			return attachmentsResult.Error;
		var attachments = attachmentsResult.Value;

		var messageId = ids.NextId();

		var messageResult = Message.Create(
			id: messageId,
			channelId: command.ChannelId,
			authorId: userId,
			content: command.Content,
			replyToId: command.ReplyToId,
			now: clock.UtcNow,
			hasAttachments: attachments.Count > 0);
		if (messageResult.IsFailure)
			return messageResult.Error;

		var message = messageResult.Value;
		await repository.AddAsync(message, command.Nonce, attachments, cancellationToken);

		var response = MessageResponse.From(message, command.Nonce, attachments);

		await eventBus.PublishAsync(
			new ChatMessageSent(
				ChannelId: command.ChannelId,
				GuildId: membership.GuildId,
				AuthorId: userId,
				MessageId: messageId,
				Content: message.Content ?? string.Empty,
				Mentions: []),
			cancellationToken);

		await broadcaster.BroadcastMessageAsync(command.ChannelId, response, cancellationToken);

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
