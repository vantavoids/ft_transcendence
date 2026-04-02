using Auth.Domain.Results;
using Auth.Domain.Enums;

namespace Auth.Domain.ValueObjects;

public sealed record OAuthIdentity
{
    private OAuthIdentity(OAuthProvider provider, string oauthId)
    {
        Provider = provider;
        Id = oauthId;
    }

    public const uint MaxIdLen = 255;

    public OAuthProvider Provider { get; }
    public string        Id { get; }

#nullable enable
    public static Result<OAuthIdentity> Create(OAuthProvider? provider, string? oauthId)
    {
        if (provider is null)
            return AuthFailures.InvalidOAuthProvider;

        if (string.IsNullOrWhiteSpace(oauthId))
            return AuthFailures.InvalidOAuthId;

        var normalizedOAuthId = oauthId.Trim();

        if (normalizedOAuthId.Length > MaxIdLen)
            return AuthFailures.InvalidOAuthId;

        return new OAuthIdentity((OAuthProvider)provider, normalizedOAuthId);
    }
}