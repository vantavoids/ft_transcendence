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

        var payloadResult = DecodeIdToken(tokenResult.Value.IdToken);
        if (payloadResult.IsFailure)
            return payloadResult.Error;

        var payload = payloadResult.Value;
        return new OAuthUserInfo(payload.Sub, payload.Email, payload.EmailVerified);
    }

    private static Result<GoogleIdTokenPayload> DecodeIdToken(string idToken)
    {
        try
        {
            var parts = idToken.Split('.');
            if (parts.Length != 3)
                return AuthFailures.OAuthUpstreamError;

            var base64 = parts[1].Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

            var payload = JsonSerializer.Deserialize<GoogleIdTokenPayload>(Convert.FromBase64String(base64));
            if (payload is null || string.IsNullOrEmpty(payload.Sub))
                return AuthFailures.OAuthUpstreamError;

            return payload;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return AuthFailures.OAuthUpstreamError;
        }
    }

    private sealed record GoogleTokenResp(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("id_token")]     string IdToken);

#nullable enable
    private sealed record GoogleIdTokenPayload(
        [property: JsonPropertyName("sub")]   string  Sub,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName(
                        "email_verified")]    bool?   EmailVerified);
}
