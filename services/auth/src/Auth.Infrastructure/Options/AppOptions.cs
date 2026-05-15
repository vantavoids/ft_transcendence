using System.ComponentModel.DataAnnotations;

namespace Auth.Infrastructure.Options;

public sealed class AppOptions : IOptions
{
    static string IOptions.SectionName => "App";

    [Required]
    public required string BaseUrl { get; init; }
}
