using System.Net;
using System.Net.Http.Headers;
using Auth.FunctionalTests.Infrastructure;
using Xunit;

namespace Auth.FunctionalTests.Endpoints;

public sealed class LogoutEndpointTests(AuthApiFactory factory)
    : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new() { AllowAutoRedirect = false });

    private Task<string> SeedUser(string email, string password) =>
        factory.SeedUserWithAccessTokenAsync(email, password);

    private HttpRequestMessage LogoutRequest(string? bearerToken)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/v1/logout");
        if (bearerToken is not null)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return req;
    }

    [Fact]
    public async Task ValidToken_Returns204_AndDeletesCookie()
    {
        var accessToken = await SeedUser("logout_valid@example.com", "password123");

        var resp = await _client.SendAsync(LogoutRequest(accessToken));

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var setCookie = Assert.Single(
            resp.Headers.GetValues("Set-Cookie"),
            h => h.StartsWith("refresh_token=", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("expires=", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MissingToken_Returns401()
    {
        var resp = await _client.SendAsync(LogoutRequest(bearerToken: null));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task InvalidToken_Returns401()
    {
        var resp = await _client.SendAsync(LogoutRequest("this.is.not.a.valid.jwt"));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task ValidToken_RevokesRefreshToken_InDatabase()
    {
        var (accessToken, userId) = await factory.SeedUserWithBothTokensAsync(
            "logout_db@example.com", "password123");

        await _client.SendAsync(LogoutRequest(accessToken));

        Assert.True(await factory.IsRefreshTokenRevokedAsync(userId));
    }
}
