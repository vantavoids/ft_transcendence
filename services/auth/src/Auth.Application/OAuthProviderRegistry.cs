using Auth.Domain.Enums;
using Auth.Domain.Results;

namespace Auth.Application;

#nullable enable

public static class OAuthProviderRegistry
{
    private sealed record Entry(OAuthProvider Provider, string Value);

    private static readonly Entry[] Entries =
    [
        new(OAuthProvider.Github, "github"),
        new(OAuthProvider.Google, "google"),
        new(OAuthProvider.FortyTwo, "42")
    ];

    public static bool IsSupported(OAuthProvider provider) =>
        Entries.Any(x => x.Provider == provider);

    public static string? ToValue(OAuthProvider provider) =>
        Entries.FirstOrDefault(x => x.Provider == provider)?.Value;

    public static OAuthProvider? FromValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().ToLowerInvariant();

        return Entries.FirstOrDefault(x => x.Value == normalized)?.Provider;
    }
}
