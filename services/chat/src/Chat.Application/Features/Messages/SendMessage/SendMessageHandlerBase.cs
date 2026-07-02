using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Attachments;
using Chat.Domain.Messages;
using Chat.Domain.Results;

namespace Chat.Application.Features.Messages.SendMessage;

/// <summary>marker for handlers whose precheck doesn't need to carry anything forward (DM send)</summary>
public readonly record struct NoContext;

/// <summary>
/// Owns the entire "send a message" skeleton. <typeparamref name="TContext"/> carries
/// whatever a subclass's precheck needs to hand to its own event/notify step (e.g. the GuildId
/// a channel send needs for ChatMessageSent); use <see cref="NoContext"/> when there's nothing to carry.
/// </summary>
internal abstract class SendMessageHandlerBase<TCommand, TResponse, TContext>(
	ICurrentUser currentUser,
	IMessageRepository repository,
	IAttachmentRepository attachmentRepository,
	ISnowflakeIdGenerator ids,
	IClock clock)
	: ICommandHandler<TCommand, Result<TResponse>>
	where TCommand : class, ISendMessageCommand, ICommand<Result<TResponse>>
	where TResponse : class, IMessageWireResponse<TResponse>
{
	private const int MaxNonceLen = 64;
	private const int MaxAttachments = 10;

	protected long AuthorId { get; } = currentUser.UserId;
	protected IMessageRepository Repository { get; } = repository;
	protected ISnowflakeIdGenerator Ids { get; } = ids;

	public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
	{
		if (command.Nonce is { Length: > MaxNonceLen })
			return MessageFailures.NonceTooLong;

		var precheck = await PrecheckAsync(command, cancellationToken);
		if (precheck.IsFailure)
			return precheck.Error;
		var context = precheck.Value;

		// nonce dedup runs before the container is even resolved: a retried DM
		// send must not create a conversation row (or re-consume drafts) on a hit
		if (command.Nonce is not null)
		{
			var existingId = await FindNonceAsync(command, command.Nonce, cancellationToken);
			if (existingId is not null)
			{
				var existing = await Repository.GetByIdAsync(existingId.Value, cancellationToken);

				if (existing is not null)
					return TResponse.From(
						existing, command.Nonce,
						await attachmentRepository.GetMessageAttachmentsAsync(existing.ContainerId, existing.IsDirectMessage, existing.Id, cancellationToken));
			}
		}

		var containerId = await ResolveContainerIdAsync(command, cancellationToken);

		if (command.ReplyToId is not null
			&& !await ReplyTargetExistsAsync(containerId, command.ReplyToId.Value, cancellationToken))
			return MessageFailures.InvalidReplyTarget;

		var attachmentsResult = await ResolveAttachmentsAsync(command.AttachmentIds, AuthorId, cancellationToken);
		if (attachmentsResult.IsFailure)
			return attachmentsResult.Error;
		var attachments = attachmentsResult.Value;

		var messageResult = CreateMessage(command, containerId, Ids.NextId(), attachments.Count > 0, clock.UtcNow);
		if (messageResult.IsFailure)
			return messageResult.Error;

		var message = messageResult.Value;
		await Repository.AddAsync(message, command.Nonce, attachments, cancellationToken);

		var response = TResponse.From(message, command.Nonce, attachments);

		await PublishAndNotifyAsync(command, context, message, response, cancellationToken);

		return response;
	}

	/// <summary>authorization; whatever it resolves that's needed later (e.g. GuildId) flows out as TContext</summary>
	protected abstract Task<Result<TContext>> PrecheckAsync(TCommand command, CancellationToken ct);

	protected abstract Task<long?> FindNonceAsync(TCommand command, string nonce, CancellationToken ct);

	/// <summary>the channel id (already known) or the DM conversation id (resolved/created here)</summary>
	protected abstract Task<long> ResolveContainerIdAsync(TCommand command, CancellationToken ct);

	protected abstract Result<Message> CreateMessage(TCommand command, long containerId, long messageId, bool hasAttachments, DateTimeOffset now);

	protected abstract Task PublishAndNotifyAsync(TCommand command, TContext context, Message message, TResponse response, CancellationToken ct);

	private async Task<Result<IReadOnlyList<AttachmentMetadata>>> ResolveAttachmentsAsync(
		IReadOnlyList<long> attachmentIds, long userId, CancellationToken ct)
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

	/// <summary>true if replyToId resolves to a non-deleted message inside containerId</summary>
	private async Task<bool> ReplyTargetExistsAsync(long containerId, long replyToId, CancellationToken ct)
	{
		var target = await Repository.GetByIdAsync(replyToId, ct);
		return target is not null && !target.IsDeleted && target.ContainerId == containerId;
	}
}
