using System.Text.Json.Serialization;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Auth.Infrastructure.Options.OAuth;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.OAuth;

[OAuthProviderKey(OAuthProvider.FortyTwo)]
internal sealed class FortyTwoOAuthProvider(
    HttpClient http,
    IOptions<FortyTwoOAuthOptions> options) : OAuthProviderBase(http)
{
    private readonly FortyTwoOAuthOptions _opts = options.Value;

    public override Uri BuildAuthorizationUrl(string state)
    {
        var qs = BuildQueryString(
            ("client_id", _opts.ClientId),
            ("redirect_uri", _opts.RedirectUri),
            ("response_type", "code"),
            ("scope", "public"),
            ("state", state));
        return new Uri($"{_opts.AuthorizationEndpoint}?{qs}");
    }

    public override async Task<Result<OAuthUserInfo>> ExchangeCodeAsync(
        string code, string state, CancellationToken cancellationToken = default)
    {
        var tokenResult = await FetchToken<FortyTwoTokenResp>(
            _opts.TokenEndpoint,
            new() {
                ["grant_type"]    = "authorization_code",
                ["client_id"]     = _opts.ClientId,
                ["client_secret"] = _opts.ClientSecret,
                ["code"]          = code,
                ["redirect_uri"]  = _opts.RedirectUri,
                ["state"]         = state,
            },
            cancellationToken);
        if (tokenResult.IsFailure)
            return tokenResult.Error;

        var userResult = await FetchAuthenticated<FortyTwoUserResp>(
            _opts.UserEndpoint, tokenResult.Value.AccessToken, cancellationToken);
        if (userResult.IsFailure)
            return userResult.Error;

        var user = userResult.Value;
        return new OAuthUserInfo(user.Id.ToString(), user.Email, user.Email is not null);
    }

    private sealed record FortyTwoTokenResp(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")]   string TokenType,
        [property: JsonPropertyName("scope")]        string Scope,
        [property: JsonPropertyName("expires_in")]   int ExpiresIn,
        [property: JsonPropertyName("created_at")]   long CreatedAt);

    private sealed record FortyTwoUserResp(
        [property: JsonPropertyName("id")]    long    Id,
        [property: JsonPropertyName("email")] string? Email);
}
