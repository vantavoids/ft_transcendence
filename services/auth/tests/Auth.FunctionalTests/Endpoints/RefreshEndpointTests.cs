using System.Net;
using Auth.FunctionalTests.Infrastructure;
using Xunit;

namespace Auth.FunctionalTests.Endpoints;

public sealed class RefreshEndpointTests(AuthApiFactory factory)
    : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new() { AllowAutoRedirect = false });

    private Task<string> SeedUser(string email, string password) =>
        factory.SeedUserWithRefreshTokenAsync(email, password);

    private HttpRequestMessage RefreshRequest(string? rawToken)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/v1/refresh");
        if (rawToken is not null)
            req.Headers.Add("Cookie", $"refresh_token={rawToken}");
        return req;
    }

    [Fact]
    public async Task ValidToken_Returns200_WithNewAccessTokenAndCookie()
    {
        var rawToken = await SeedUser("refresh_valid@example.com", "password123");

        var resp = await _client.SendAsync(RefreshRequest(rawToken));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var json = await resp.ReadJsonAsync();
        Assert.NotEmpty(json["access_token"]!.GetValue<string>());

        var setCookie = Assert.Single(
            resp.Headers.GetValues("Set-Cookie"),
            h => h.StartsWith("refresh_token=", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("HttpOnly", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidToken_RotatesCookie()
    {
        var rawToken = await SeedUser("refresh_rotate@example.com", "password123");

        var first  = await _client.SendAsync(RefreshRequest(rawToken));
        var newToken = first.Headers.GetValues("Set-Cookie")
            .Single(h => h.StartsWith("refresh_token=", StringComparison.OrdinalIgnoreCase))
            .Split(';')[0]
            .Split('=', 2)[1];

        var second = await _client.SendAsync(RefreshRequest(newToken));

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task MissingCookie_Returns401()
    {
        var resp = await _client.SendAsync(RefreshRequest(rawToken: null));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task UnknownToken_Returns401()
    {
        var resp = await _client.SendAsync(RefreshRequest("garbage-token-xyz"));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task UsedToken_Returns401()
    {
        var rawToken = await SeedUser("refresh_used@example.com", "password123");

        await _client.SendAsync(RefreshRequest(rawToken));
        var resp = await _client.SendAsync(RefreshRequest(rawToken));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
