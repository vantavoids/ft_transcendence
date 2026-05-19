using System.Text.Json.Serialization;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Auth.Infrastructure.Options.OAuth;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.OAuth;

[OAuthProviderKey(OAuthProvider.Github)]
internal sealed class GithubOAuthProvider(
    HttpClient http,
    IOptions<GithubOAuthOptions> options) : OAuthProviderBase(http)
{
    private readonly GithubOAuthOptions _opts = options.Value;

    public override Uri BuildAuthorizationUrl(string state)
    {
        var qs = BuildQueryString(
            ("client_id", _opts.ClientId),
            ("redirect_uri", _opts.RedirectUri),
            ("scope", "read:user user:email"),
            ("state", state));
        return new Uri($"{_opts.AuthorizationEndpoint}?{qs}");
    }

    public override async Task<Result<OAuthUserInfo>> ExchangeCodeAsync(
        string code, string _, CancellationToken cancellationToken = default)
    {
        var tokenResult = await FetchToken<GithubTokenResp>(
            _opts.TokenEndpoint,
            new() {
                ["client_id"]     = _opts.ClientId,
                ["client_secret"] = _opts.ClientSecret,
                ["code"]          = code,
                ["redirect_uri"]  = _opts.RedirectUri,
            },
            cancellationToken);
        if (tokenResult.IsFailure)
            return tokenResult.Error;
        var token = tokenResult.Value;

        var userResult = await FetchAuthenticated<GithubUserResp>(
            _opts.UserEndpoint, token.AccessToken, cancellationToken);
        if (userResult.IsFailure)
            return userResult.Error;
        var user = userResult.Value;

        var email    = user.Email;
        var verified = false;

        if (token.Scopes.Split(',').Contains("user:email"))
        {
            var fieldsResult = await GetEmailFields(token.AccessToken, cancellationToken);
            if (fieldsResult.IsFailure)
                return fieldsResult.Error;

            var (emailVerified, primaryEmail) = fieldsResult.Value;
            email    = primaryEmail  ?? email;
            verified = emailVerified ?? false;
        }

        return new OAuthUserInfo(user.Id.ToString(), email, verified);
    }

#nullable enable
    private async Task<Result<(bool? Verified, string? Email)>> GetEmailFields(string accessToken, CancellationToken ct)
    {
        var result = await FetchAuthenticated<IReadOnlyList<GithubEmailResp>>(
            _opts.EmailEndpoint, accessToken, ct);
        if (result.IsFailure)
            return result.Error;

        var best = result.Value.FirstOrDefault(e => e.Primary)
                ?? result.Value.FirstOrDefault(e => e.Verified);
        return (best?.Verified, best?.Email);
    }

    private sealed record GithubTokenResp(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("scope")]        string Scopes);

    private sealed record GithubUserResp(
        [property: JsonPropertyName("id")]    long    Id,
        [property: JsonPropertyName("email")] string? Email);

    private sealed record GithubEmailResp(
        [property: JsonPropertyName("email")]    string Email,
        [property: JsonPropertyName("primary")]  bool   Primary,
        [property: JsonPropertyName("verified")] bool   Verified);
}
