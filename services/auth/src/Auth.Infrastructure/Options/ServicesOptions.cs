using System.ComponentModel.DataAnnotations;

namespace Auth.Infrastructure.Options;

public sealed class ServicesOptions: IOptions
{
    static string IOptions.SectionName => "Services";

    [Required] public required string GuildService { get; init; }
}
