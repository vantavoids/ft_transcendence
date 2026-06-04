using System.ComponentModel.DataAnnotations;

namespace Guild.Infrastructure.Options;

internal sealed class UserServiceOptions : IOptions
{
	static string IOptions.SectionName => "UserService";

	[Required] public required string BaseUrl { get; init; }
}
