namespace Chat.Application.Abstractions;

/// <summary>
/// builds the public download URL clients render for an attachment. the URL routes
/// back through <c>GET /attachments/{id}/{filename}</c> (auth-checked on every hit),
/// so it is derived from the gateway base URL, not the storage endpoint
/// </summary>
public interface IAttachmentUrlFactory
{
	string BuildUrl(long attachmentId, string filename);
}
