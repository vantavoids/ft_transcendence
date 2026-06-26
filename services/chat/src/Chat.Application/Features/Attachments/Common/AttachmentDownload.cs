namespace Chat.Application.Features.Attachments.Common;

/// <summary>
/// the payload of a successful <c>GET /attachments/{id}/{filename}</c>: an open
/// read stream plus the headers the endpoint streams it back with. Caller owns
/// disposing <see cref="Content"/>
/// </summary>
public sealed record AttachmentDownload(Stream Content, string MimeType, string Filename);
