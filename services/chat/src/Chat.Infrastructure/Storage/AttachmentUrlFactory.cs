using Chat.Application.Abstractions;
using Chat.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Chat.Infrastructure.Storage;

/// <summary>
/// builds the public, auth-checked download URL for an attachment off the gateway
/// base API URL, e.g. <c>https://host/api/chat/attachments/{id}/{filename}</c>
/// </summary>
internal sealed class AttachmentUrlFactory(IOptions<BackendConfigurationOptions> options) : IAttachmentUrlFactory
{
	private readonly string _baseApiUrl = options.Value.BaseApiUrl.TrimEnd('/');

	// the /v1 segment is required: the gateway routes on /api/{service}/v{N}/...,
	// so a versionless URL (as drawn in the contract) is not reachable
	public string BuildUrl(long attachmentId, string filename) =>
		$"{_baseApiUrl}/chat/v1/attachments/{attachmentId}/{Uri.EscapeDataString(filename)}";
}
