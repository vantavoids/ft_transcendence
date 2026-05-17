using System.Net;
using System.Net.Http.Json;
using Auth.FunctionalTests.Infrastructure;
using Xunit;

namespace Auth.FunctionalTests.Endpoints;

public sealed class RegisterEndpointTests(AuthApiFactory factory)
    : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new() { AllowAutoRedirect = false });

    private static StringContent JsonBody(string email, string password) =>
        new($$"""{"email":"{{email}}","password":"{{password}}"}""",
            System.Text.Encoding.UTF8, "application/json");

    [Fact]
    public async Task ValidRequest_Returns201_WithTokensAndCookie()
    {
        var resp = await _client.PostAsync("/v1/register",
            JsonBody("register_valid@example.com", "password123"));

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var json = await resp.ReadJsonAsync();
        Assert.NotNull(json["user_id"]?.GetValue<long>());
        Assert.NotEmpty(json["access_token"]!.GetValue<string>());

        var setCookie = Assert.Single(
            resp.Headers.GetValues("Set-Cookie"),
            h => h.StartsWith("refresh_token=", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("HttpOnly", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidRequest_LocationHeader_PointsToUser()
    {
        var resp = await _client.PostAsync("/v1/register",
            JsonBody("register_location@example.com", "password123"));

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var userId = (await resp.ReadJsonAsync())["user_id"]!.GetValue<long>();
        Assert.Equal($"/users/{userId}", resp.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task DuplicateEmail_Returns409()
    {
        await _client.PostAsync("/v1/register",
            JsonBody("register_dup@example.com", "password123"));

        var resp = await _client.PostAsync("/v1/register",
            JsonBody("register_dup@example.com", "other-password"));

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task InvalidEmail_Returns400()
    {
        var resp = await _client.PostAsync("/v1/register",
            JsonBody("not-an-email", "password123"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task MissingBody_Returns400()
    {
        var resp = await _client.PostAsync("/v1/register",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
