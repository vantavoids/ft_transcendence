using System.ComponentModel.DataAnnotations;

namespace Guild.Infrastructure.Options;

internal sealed class JwtOptions : IOptions
{
	static string IOptions.SectionName => "Jwt";

	[Required]
	[MinLength(32)]
	public required string SecretKey { get; init; }
}
