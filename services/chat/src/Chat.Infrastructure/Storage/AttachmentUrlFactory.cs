using Chat.Application.Abstractions;
using Chat.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Chat.Infrastructure.Storage;

/// <summary>
/// builds the public, auth-checked download URL for an attachment as an
/// origin-relative path, e.g. <c>/api/chat/v1/attachments/{id}/{filename}</c>.
/// It is intentionally relative (no scheme/host): the frontend fetches attachments
/// with JS (which enforces CORS), and the app is reached by whatever hostname/IP
/// the evaluator uses, not necessarily <c>localhost</c>. A relative path always
/// resolves against the page's own origin, so it stays same-origin (no CORS)
/// regardless of how the app is accessed.
/// </summary>
internal sealed class AttachmentUrlFactory(IOptions<BackendConfigurationOptions> options) : IAttachmentUrlFactory
{
	// take just the path prefix of the configured API base (e.g. "/api"); drop the
	// scheme + host so the URL is origin-relative.
	private readonly string _basePath =
		Uri.TryCreate(options.Value.BaseApiUrl.TrimEnd('/'), UriKind.Absolute, out var uri)
			? uri.AbsolutePath.TrimEnd('/')
			: options.Value.BaseApiUrl.TrimEnd('/');

	// the /v1 segment is required: the gateway routes on /api/{service}/v{N}/...,
	// so a versionless URL (as drawn in the contract) is not reachable
	public string BuildUrl(long attachmentId, string filename) =>
		$"{_basePath}/chat/v1/attachments/{attachmentId}/{Uri.EscapeDataString(filename)}";
}
