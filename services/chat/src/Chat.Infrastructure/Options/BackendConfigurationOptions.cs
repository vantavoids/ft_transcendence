using System.ComponentModel.DataAnnotations;

namespace Chat.Infrastructure.Options;

public sealed class BackendConfigurationOptions
{
	[Required] public required string BaseUrl { get; init; }
	[Required] public required string BaseApiUrl { get; init; }
}
