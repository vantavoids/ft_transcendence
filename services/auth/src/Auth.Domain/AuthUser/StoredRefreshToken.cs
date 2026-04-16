using Auth.Domain.Results;

namespace Auth.Domain.ValueObjects;

public sealed record StoredRefreshToken
{
    private StoredRefreshToken(
        string hash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        bool revoked)
    {
        Hash = hash;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        Revoked = revoked;
    }

    public const uint MaxHashLen = 255;

    public string         Hash { get; }
    public DateTimeOffset IssuedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public bool           Revoked { get; }

#nullable enable

    public static Result<StoredRefreshToken> Create(
        string? hash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return AuthFailures.InvalidRefreshToken;

        if (expiresAt <= issuedAt)
            return AuthFailures.InvalidRefreshToken;

        var normalized = hash.Trim();
        if (normalized.Length > MaxHashLen)
            return AuthFailures.InvalidRefreshToken;

        return new StoredRefreshToken(
            normalized,
            issuedAt,
            expiresAt,
            revoked: false);
    }

    public bool IsActive(DateTimeOffset now) =>
        Revoked is false && ExpiresAt > now;

    public StoredRefreshToken Revoke() =>
        new(Hash, IssuedAt, ExpiresAt, revoked: true);
}
