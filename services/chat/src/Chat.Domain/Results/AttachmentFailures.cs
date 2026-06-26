using Chat.Domain.Attachments;

namespace Chat.Domain.Results;

public static class AttachmentFailures
{
	public static readonly Failure FileRequired =
		new("Attachment.FileRequired", "A file is required.");

	public static readonly Failure FileEmpty =
		new("Attachment.FileEmpty", "Uploaded file is empty.");

	public static readonly Failure FileTooLarge =
		new("Attachment.FileTooLarge",
			$"File exceeds the {DraftAttachment.MaxSizeBytes / (1024 * 1024)} MB limit.");

	public static readonly Failure BlockedMimeType =
		new("Attachment.BlockedMimeType", "This file type is not allowed.");

	public static readonly Failure NotFound =
		new("Attachment.NotFound", "Attachment not found.");

	public static readonly Failure NotAuthorized =
		new("Attachment.NotAuthorized", "Not authorized to view this attachment.");

	public static readonly Failure TooMany =
		new("Attachment.TooMany", "A message can carry at most 10 attachments.");

	// covers every "you can't reference this draft" case the send path rejects:
	// missing, owned by someone else, already attached, or TTL-expired
	public static readonly Failure InvalidReference =
		new("Attachment.InvalidReference",
			"Referenced attachment not found, not owned by caller, already attached, or expired.");
}
