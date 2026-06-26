namespace Chat.Domain.Attachments;

/// <summary>
/// wire-and-storage shape of a persisted attachment: the metadata a client needs
/// to render and download it. shared by draft uploads, message attachments and
/// the REST response mapping
/// </summary>
public sealed record AttachmentMetadata(
	long Id,
	string Url,
	string Filename,
	long SizeBytes,
	string MimeType);
