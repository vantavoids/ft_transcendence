using Chat.Domain.Results;

namespace Chat.Domain.Attachments;

/// <summary>
/// a file uploaded via <c>POST /attachments</c> but not yet attached to a message.
/// owned by the uploader, lives for 1 hour (enforced by the table TTL), and becomes
/// permanent once referenced by a sent message
/// </summary>
public sealed class DraftAttachment
{
	public const long MaxSizeBytes = 25L * 1024 * 1024;

	// executables / scripts per docs/contracts/chat.md
	private static readonly HashSet<string> BlockedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"application/x-msdownload",
		"application/x-executable",
		"application/x-mach-binary",
		"application/vnd.android.package-archive",
		"application/x-sh",
	};

	private DraftAttachment(
		long id,
		long uploaderId,
		string url,
		string filename,
		long sizeBytes,
		string mimeType,
		DateTimeOffset createdAt)
	{
		Id = id;
		UploaderId = uploaderId;
		Url = url;
		Filename = filename;
		SizeBytes = sizeBytes;
		MimeType = mimeType;
		CreatedAt = createdAt;
	}

	public long Id { get; }
	public long UploaderId { get; }
	public string Url { get; }
	public string Filename { get; }
	public long SizeBytes { get; }
	public string MimeType { get; }
	public DateTimeOffset CreatedAt { get; }

	public static Result<DraftAttachment> Create(
		long id,
		long uploaderId,
		string url,
		string filename,
		long sizeBytes,
		string mimeType,
		DateTimeOffset now)
	{
		if (string.IsNullOrWhiteSpace(filename))
			return AttachmentFailures.FileRequired;

		if (sizeBytes <= 0)
			return AttachmentFailures.FileEmpty;

		if (sizeBytes > MaxSizeBytes)
			return AttachmentFailures.FileTooLarge;

		var normalizedMime = string.IsNullOrWhiteSpace(mimeType)
			? "application/octet-stream"
			: mimeType;

		if (BlockedMimeTypes.Contains(normalizedMime))
			return AttachmentFailures.BlockedMimeType;

		return new DraftAttachment(id, uploaderId, url, filename, sizeBytes, normalizedMime, now);
	}

	public static DraftAttachment Reconstitute(
		long id,
		long uploaderId,
		string url,
		string filename,
		long sizeBytes,
		string mimeType,
		DateTimeOffset createdAt)
		=> new(id, uploaderId, url, filename, sizeBytes, mimeType, createdAt);

	public AttachmentMetadata ToMetadata() => new(Id, Url, Filename, SizeBytes, MimeType);
}
