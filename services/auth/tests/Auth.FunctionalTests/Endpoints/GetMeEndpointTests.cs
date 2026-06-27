using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Auth.FunctionalTests.Infrastructure;
using Xunit;

namespace Auth.FunctionalTests.Endpoints;

public sealed class GetMeEndpointTests(AuthApiFactory factory)
    : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new() { AllowAutoRedirect = false });

    private static StringContent JsonBody(string email, string password) =>
        new($$"""{"email":"{{email}}","password":"{{password}}"}""",
            Encoding.UTF8, "application/json");

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var resp = await _client.PostAsync("/v1/login", JsonBody(email, password));
        var json = await resp.ReadJsonAsync();
        return json["access_token"]!.GetValue<string>();
    }

    [Fact]
    public async Task NoToken_Returns401()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var resp = await client.GetAsync("/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task InvalidToken_Returns401()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.valid.token");

        var resp = await client.GetAsync("/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task ValidEmailUser_Returns200_WithCorrectBody()
    {
        await factory.SeedEmailUserAsync("getme_valid@example.com", "password123");
        var token = await LoginAndGetTokenAsync("getme_valid@example.com", "password123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _client.GetAsync("/v1/me");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.ReadJsonAsync();
        Assert.NotNull(json["id"]?.GetValue<string>());
        Assert.Equal("getme_valid@example.com", json["email"]!.GetValue<string>());
        Assert.False(json["email_verified"]!.GetValue<bool>());
        Assert.Empty(json["oauth_providers"]!.AsArray());
    }

    [Fact]
    public async Task OAuthUser_Returns200_WithProviderAndNullEmail()
    {
        var token = await factory.SeedOAuthUserWithAccessTokenAsync(Domain.AuthUser.OAuthProvider.Github, "gh-123");
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.GetAsync("/v1/me");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.ReadJsonAsync();
        Assert.Null(json["email"]?.GetValue<string>());
        var providers = json["oauth_providers"]!.AsArray().Select(x => x!.GetValue<string>()).ToArray();
        Assert.Equal(["github"], providers);
    }
}
