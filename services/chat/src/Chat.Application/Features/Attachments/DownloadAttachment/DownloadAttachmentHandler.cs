using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Features.Attachments.Common;
using Chat.Application.Features.Attachments.UploadAttachment;
using Chat.Domain.Attachments;
using Chat.Domain.Results;

namespace Chat.Application.Features.Attachments.DownloadAttachment;

internal sealed class DownloadAttachmentHandler(
	ICurrentUser currentUser,
	IAttachmentRepository repository,
	IMessageRepository messageRepository,
	IGuildClient guildClient,
	IObjectStore objectStore)
	: IQueryHandler<DownloadAttachmentQuery, Result<AttachmentDownload>>
{
	// mirrors the Guild Service permission bitmask; see SendChannelMessageHandler
	private const long ReadMessages = 1L << 1;
	private const long Administrator = 1L << 8;

	public async Task<Result<AttachmentDownload>> HandleAsync(
		DownloadAttachmentQuery query,
		CancellationToken cancellationToken = default)
	{
		var location = await repository.GetLocationAsync(query.Id, cancellationToken);

		// no lookup row -> still a draft (or never existed): only the uploader may fetch
		if (location is null)
			return await DownloadDraftAsync(query, cancellationToken);

		if (location.IsDm)
			return await DownloadDmAttachmentAsync(query, location, cancellationToken);

		return await DownloadChannelAttachmentAsync(query, location, cancellationToken);
	}

	private async Task<Result<AttachmentDownload>> DownloadChannelAttachmentAsync(
		DownloadAttachmentQuery query,
		AttachmentLocation location,
		CancellationToken ct)
	{
		var channelId = location.ContainerId;

		var membership = await guildClient.GetMembershipAsync(channelId, currentUser.UserId, ct);
		if (membership is null || !membership.IsMember)
			return AttachmentFailures.NotAuthorized;

		if ((membership.Permissions & (ReadMessages | Administrator)) == 0)
			return AttachmentFailures.NotAuthorized;

		var metadata = await repository.GetAttachmentAsync(
			channelId, isDm: false, location.MessageId, query.Id, ct);
		if (metadata is null)
			return AttachmentFailures.NotFound;

		return await OpenAsync(metadata, query.Filename, ct);
	}

	private async Task<Result<AttachmentDownload>> DownloadDmAttachmentAsync(
		DownloadAttachmentQuery query,
		AttachmentLocation location,
		CancellationToken ct)
	{
		var conversationId = location.ContainerId;

		var message = await messageRepository.GetByIdAsync(location.MessageId, ct);
		if (message is null)
			return AttachmentFailures.NotAuthorized;

		var userId = currentUser.UserId;
		if (message.AuthorId != userId && message.RecipientId != userId)
			return AttachmentFailures.NotAuthorized;

		var metadata = await repository.GetAttachmentAsync(conversationId, isDm: true, location.MessageId, query.Id, ct);
		if (metadata is null)
			return AttachmentFailures.NotFound;

		return await OpenAsync(metadata, query.Filename, ct);
	}

	private async Task<Result<AttachmentDownload>> DownloadDraftAsync(
		DownloadAttachmentQuery query,
		CancellationToken ct)
	{
		var draft = await repository.GetDraftAsync(query.Id, ct);
		if (draft is null)
			return AttachmentFailures.NotFound;

		if (draft.UploaderId != currentUser.UserId)
			return AttachmentFailures.NotAuthorized;

		return await OpenAsync(draft.ToMetadata(), query.Filename, ct);
	}

	private async Task<Result<AttachmentDownload>> OpenAsync(
		AttachmentMetadata metadata,
		string requestedFilename,
		CancellationToken ct)
	{
		// the filename is part of the canonical URL; a mismatch is treated as a miss
		if (!string.Equals(metadata.Filename, requestedFilename, StringComparison.Ordinal))
			return AttachmentFailures.NotFound;

		var stream = await objectStore.GetAsync(UploadAttachmentHandler.ObjectKey(metadata.Id), ct);
		if (stream is null)
			return AttachmentFailures.NotFound;

		return new AttachmentDownload(stream, metadata.MimeType, metadata.Filename);
	}
}
