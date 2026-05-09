using System.ComponentModel.DataAnnotations;

namespace Chat.Infrastructure.Options;

public sealed class ServicesOptions
{
	[Required] public required string GuildService { get; init; }
}
