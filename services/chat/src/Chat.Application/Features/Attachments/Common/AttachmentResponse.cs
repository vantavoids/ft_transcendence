using Chat.Domain.Attachments;

namespace Chat.Application.Features.Attachments.Common;

/// <summary>
/// wire shape for an attachment, mirroring the <c>attachments[]</c> entries in the
/// contract. <c>id</c> is a quoted snowflake; <c>url</c> routes through the
/// auth-checked download endpoint
/// </summary>
public sealed record AttachmentResponse(
	string Id,
	string Url,
	string Filename,
	long SizeBytes,
	string MimeType)
{
	public static AttachmentResponse From(AttachmentMetadata m) => new(
		Id: m.Id.ToString(),
		Url: m.Url,
		Filename: m.Filename,
		SizeBytes: m.SizeBytes,
		MimeType: m.MimeType);
}
