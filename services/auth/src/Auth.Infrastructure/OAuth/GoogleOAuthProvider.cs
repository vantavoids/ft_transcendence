using System.Text.Json;
using System.Text.Json.Serialization;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Auth.Infrastructure.Options.OAuth;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.OAuth;

[OAuthProviderKey(OAuthProvider.Google)]
internal sealed class GoogleOAuthProvider(
    HttpClient http,
    IOptions<GoogleOAuthOptions> options) : OAuthProviderBase(http)
{
    private readonly GoogleOAuthOptions _opts = options.Value;

    public override Uri BuildAuthorizationUrl(string state)
    {
        var qs = BuildQueryString(
            ("client_id", _opts.ClientId),
            ("redirect_uri", _opts.RedirectUri),
            ("response_type", "code"),
            ("scope", "openid email"),
            ("state", state));
        return new Uri($"{_opts.AuthorizationEndpoint}?{qs}");
    }

    public override async Task<Result<OAuthUserInfo>> ExchangeCodeAsync(
        string code, string _, CancellationToken cancellationToken = default)
    {
        var tokenResult = await FetchToken<GoogleTokenResp>(
            _opts.TokenEndpoint,
            new() {
                ["client_id"]     = _opts.ClientId,
                ["client_secret"] = _opts.ClientSecret,
                ["code"]          = code,
                ["redirect_uri"]  = _opts.RedirectUri,
                ["grant_type"]    = "authorization_code",
            },
            cancellationToken);
        if (tokenResult.IsFailure)
            return tokenResult.Error;

        var payload = DecodeIdToken(tokenResult.Value.IdToken);
        return new OAuthUserInfo(payload.Sub, payload.Email, payload.EmailVerified);
    }

    private static GoogleIdTokenPayload DecodeIdToken(string idToken)
    {
        var parts = idToken.Split('.');
        if (parts.Length != 3)
            throw new FormatException("Invalid JWT format.");

        var base64 = parts[1].Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

        return JsonSerializer.Deserialize<GoogleIdTokenPayload>(Convert.FromBase64String(base64))!;
    }

    private sealed record GoogleTokenResp(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("id_token")]     string IdToken);

    private sealed record GoogleIdTokenPayload(
        [property: JsonPropertyName("sub")]   string  Sub,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName(
                        "email_verified")]    bool?   EmailVerified);
}
