using System.ComponentModel.DataAnnotations;

namespace Guild.Infrastructure.Options;

internal sealed class BackendConfigurationOptions : IOptions
{
	static string IOptions.SectionName => "BackendConfiguration";

	[Required] public required string BaseUrl { get; init; }
	[Required] public required string BaseApiUrl { get; init; }
}
