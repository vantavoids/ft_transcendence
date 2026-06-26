using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Features.Attachments.Common;
using Chat.Domain.Attachments;
using Chat.Domain.Results;

namespace Chat.Application.Features.Attachments.UploadAttachment;

internal sealed class UploadAttachmentHandler(
	ICurrentUser currentUser,
	ISnowflakeIdGenerator ids,
	IClock clock,
	IObjectStore objectStore,
	IAttachmentUrlFactory urlFactory,
	IAttachmentRepository repository)
	: ICommandHandler<UploadAttachmentCommand, Result<AttachmentResponse>>
{
	public async Task<Result<AttachmentResponse>> HandleAsync(
		UploadAttachmentCommand command,
		CancellationToken cancellationToken = default)
	{
		var id = ids.NextId();
		var url = urlFactory.BuildUrl(id, command.FileName);

		// validate size / MIME before touching object storage so a rejected upload
		// never leaves an orphaned blob behind
		var draftResult = DraftAttachment.Create(
			id: id,
			uploaderId: currentUser.UserId,
			url: url,
			filename: command.FileName,
			sizeBytes: command.Length,
			mimeType: command.ContentType ?? string.Empty,
			now: clock.UtcNow);
		if (draftResult.IsFailure)
			return draftResult.Error;

		var draft = draftResult.Value;

		await objectStore.PutAsync(
			ObjectKey(draft.Id), command.Content, draft.MimeType, draft.SizeBytes, cancellationToken);
		await repository.AddDraftAsync(draft, cancellationToken);

		return AttachmentResponse.From(draft.ToMetadata());
	}

	// object storage is keyed by the attachment snowflake; the filename only lives
	// in metadata and the public URL
	internal static string ObjectKey(long attachmentId) => attachmentId.ToString();
}
