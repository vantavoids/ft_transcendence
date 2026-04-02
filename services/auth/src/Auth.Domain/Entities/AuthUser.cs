using Auth.Domain.Enums;
using Auth.Domain.Results;
using Auth.Domain.ValueObjects;

namespace Auth.Domain.Entities;

#nullable enable

public sealed class AuthUser
{
    private AuthUser(
        long id,
        Email? email,
        string? passwordHash,
        OAuthIdentity? oauthIdentity,
        StoredRefreshToken? refreshToken,
        DateTimeOffset? deletedAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;

        Email = email;
        PasswordHash = passwordHash;
        OAuthIdentity = oauthIdentity;

        RefreshToken = refreshToken;

        DeletedAt = deletedAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public long                Id { get; private set; }

    /* Credentials */
    public Email?              Email { get; private set; }
    public string?             PasswordHash { get; private set; }
    public OAuthIdentity?      OAuthIdentity { get; private set; }

    public StoredRefreshToken? RefreshToken { get; private set; }

    /* DateTimes */
    public DateTimeOffset?     DeletedAt { get; private set; }
    public DateTimeOffset      CreatedAt { get; private set; }
    public DateTimeOffset      UpdatedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    public bool HasPasswordCredentials =>
        Email is not null && PasswordHash is not null;
    public bool HasOAuthCredentials =>
        OAuthIdentity is not null;

    public static Result<AuthUser> CreateEmailPasswordUser(
        long id,
        string ?email,
        string ?passwordHash,
        DateTimeOffset now)
    {
        if (id <= 0)
            return AuthFailures.InvalidAuthUserState;

        if (string.IsNullOrWhiteSpace(passwordHash))
            return AuthFailures.InvalidAuthUserState;

        var mailResult = Email.Create(email);
        if (mailResult.IsFailure)
            return mailResult.Error;

        return new AuthUser(
            id: id,
            email: mailResult.Value,
            passwordHash: passwordHash,
            oauthIdentity: null,
            refreshToken: null,
            deletedAt: null,
            createdAt: now,
            updatedAt: now);
    }

    public static Result<AuthUser> CreateOAuthUser(
        long id,
        OAuthProvider ?oauthProvider,
        string ?oauthId,
        DateTimeOffset now)
    {
        if (id <= 0)
            return AuthFailures.InvalidAuthUserState;

        var oauthResult = OAuthIdentity.Create(oauthProvider, oauthId);
        if (oauthResult.IsFailure)
            return oauthResult.Error;

        return new AuthUser(
            id: id,
            email: null,
            passwordHash: null,
            oauthIdentity: oauthResult.Value,
            refreshToken: null,
            deletedAt: null,
            createdAt: now,
            updatedAt: now);
    }

    public Result SetRefreshToken(
        string? hashedTk,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        var refreshTkResult = StoredRefreshToken.Create(hashedTk, issuedAt, expiresAt);

        if (refreshTkResult.IsFailure)
            return refreshTkResult.Error;
        RefreshToken = refreshTkResult.Value;

        UpdatedAt = issuedAt;
        return Result.Ok();
    }

    public void RevokeRefreshToken(DateTimeOffset now)
    {
        if (RefreshToken is not null)
            RefreshToken = RefreshToken.Revoke();

        UpdatedAt = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        DeletedAt = now;
        UpdatedAt = now;
    }
}