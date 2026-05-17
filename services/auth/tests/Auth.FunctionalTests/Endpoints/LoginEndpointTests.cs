using System.Net;
using Auth.FunctionalTests.Infrastructure;
using Xunit;

namespace Auth.FunctionalTests.Endpoints;

public sealed class LoginEndpointTests(AuthApiFactory factory)
    : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new() { AllowAutoRedirect = false });

    private static StringContent JsonBody(string email, string password) =>
        new($$"""{"email":"{{email}}","password":"{{password}}"}""",
            System.Text.Encoding.UTF8, "application/json");

    private Task SeedUser(string email, string password) =>
        factory.SeedEmailUserAsync(email, password);

    [Fact]
    public async Task ValidCredentials_Returns200_WithTokensAndCookie()
    {
        await SeedUser("login_valid@example.com", "password123");

        var resp = await _client.PostAsync("/v1/login",
            JsonBody("login_valid@example.com", "password123"));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var json = await resp.ReadJsonAsync();
        Assert.NotNull(json["user_id"]?.GetValue<long>());
        Assert.NotEmpty(json["access_token"]!.GetValue<string>());

        var setCookie = Assert.Single(
            resp.Headers.GetValues("Set-Cookie"),
            h => h.StartsWith("refresh_token=", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("HttpOnly", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidCredentials_RefreshToken_IsRotated()
    {
        await SeedUser("login_rotate@example.com", "password123");

        var first  = await _client.PostAsync("/v1/login", JsonBody("login_rotate@example.com", "password123"));
        var second = await _client.PostAsync("/v1/login", JsonBody("login_rotate@example.com", "password123"));

        var cookie1 = first.Headers.GetValues("Set-Cookie")
            .Single(h => h.StartsWith("refresh_token=", StringComparison.OrdinalIgnoreCase));
        var cookie2 = second.Headers.GetValues("Set-Cookie")
            .Single(h => h.StartsWith("refresh_token=", StringComparison.OrdinalIgnoreCase));

        Assert.NotEqual(cookie1, cookie2);
    }

    [Fact]
    public async Task WrongPassword_Returns401()
    {
        await SeedUser("login_wrongpw@example.com", "correct-password");

        var resp = await _client.PostAsync("/v1/login",
            JsonBody("login_wrongpw@example.com", "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task UnknownEmail_Returns401()
    {
        var resp = await _client.PostAsync("/v1/login",
            JsonBody("nobody@example.com", "password123"));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task MissingBody_Returns400()
    {
        var resp = await _client.PostAsync("/v1/login",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
