using System.ComponentModel.DataAnnotations;

namespace Auth.Infrastructure.Options.OAuth;

public sealed class OAuthStateCookieOptions : IOptions
{
    static string IOptions.SectionName => "OAuthStateCookie";

    [Required]
    public required string CookieName { get; init; }

    [Required, Range(1, int.MaxValue)]
    public required int TtlMinutes { get; init; }

    public bool HttpOnly { get; init; } = true;
    public bool Secure { get; init; } = true;
}
