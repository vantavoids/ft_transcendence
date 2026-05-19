using System.Net;
using System.Text;
using Auth.Domain.Results;
using Auth.Infrastructure.OAuth;
using Auth.Infrastructure.Options.OAuth;
using Microsoft.Extensions.Options;
using Xunit;

namespace Auth.UnitTests.Infrastructure;

public sealed class GoogleOAuthProviderTests
{
    private static GoogleOAuthProvider BuildProvider(string idToken)
    {
        var json = $$"""{"access_token":"tok","id_token":"{{idToken}}"}""";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var opts = Options.Create(new GoogleOAuthOptions
        {
            ClientId              = "id",
            ClientSecret          = "secret",
            RedirectUri           = "https://test.example.com/cb",
            TokenEndpoint         = "https://oauth.test/token",
            AuthorizationEndpoint = "https://oauth.test/auth",
            UserEndpoint          = "null",
        });
        return new GoogleOAuthProvider(new HttpClient(new FakeHttpMessageHandler(response)), opts);
    }

    private static string Base64UrlEncode(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string BuildIdToken(string payloadJson)
        => $"{Base64UrlEncode("{\"alg\":\"RS256\"}")}.{Base64UrlEncode(payloadJson)}.fakesig";

    [Fact]
    public async Task ExchangeCode_ValidToken_ReturnsUserInfo()
    {
        var idToken = BuildIdToken("{\"sub\":\"99\",\"email\":\"u@test.com\",\"email_verified\":true}");
        var provider = BuildProvider(idToken);

        var result = await provider.ExchangeCodeAsync("code", "state");

        Assert.True(result.Succeeded);
        Assert.Equal("99", result.Value.ProviderId);
        Assert.Equal("u@test.com", result.Value.Email);
        Assert.True(result.Value.EmailVerified);
    }

    [Fact]
    public async Task ExchangeCode_IdTokenWithoutDots_ReturnsUpstreamError()
    {
        var provider = BuildProvider("not-a-jwt");

        var result = await provider.ExchangeCodeAsync("code", "state");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.OAuthUpstreamError.Code, result.Error.Code);
    }

    [Fact]
    public async Task ExchangeCode_InvalidBase64InPayload_ReturnsUpstreamError()
    {
        var provider = BuildProvider("header.!!!invalid!!!.sig");

        var result = await provider.ExchangeCodeAsync("code", "state");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.OAuthUpstreamError.Code, result.Error.Code);
    }

    [Fact]
    public async Task ExchangeCode_EmptySub_ReturnsUpstreamError()
    {
        var idToken = BuildIdToken("{\"sub\":\"\",\"email\":\"u@test.com\",\"email_verified\":true}");
        var provider = BuildProvider(idToken);

        var result = await provider.ExchangeCodeAsync("code", "state");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.OAuthUpstreamError.Code, result.Error.Code);
    }

    [Fact]
    public async Task ExchangeCode_NullPayload_ReturnsUpstreamError()
    {
        var idToken = $"{Base64UrlEncode("{\"alg\":\"RS256\"}")}.{Base64UrlEncode("null")}.sig";
        var provider = BuildProvider(idToken);

        var result = await provider.ExchangeCodeAsync("code", "state");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.OAuthUpstreamError.Code, result.Error.Code);
    }
}
