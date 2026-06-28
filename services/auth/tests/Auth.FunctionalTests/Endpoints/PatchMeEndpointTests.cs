using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Auth.Domain.AuthUser;
using Auth.FunctionalTests.Infrastructure;
using Xunit;

namespace Auth.FunctionalTests.Endpoints;

public sealed class PatchMeEndpointTests(AuthApiFactory factory)
    : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new() { AllowAutoRedirect = false });

    private static StringContent PatchBody(
        string? email           = null,
        string? currentPassword = null,
        string? newPassword     = null)
    {
        var parts = new List<string>();
        if (email           is not null) parts.Add($"\"email\":\"{email}\"");
        if (currentPassword is not null) parts.Add($"\"current_password\":\"{currentPassword}\"");
        if (newPassword     is not null) parts.Add($"\"new_password\":\"{newPassword}\"");
        return new StringContent("{" + string.Join(",", parts) + "}", Encoding.UTF8, "application/json");
    }

    [Fact]
    public async Task NoToken_Returns401()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var resp = await client.PatchAsync("/v1/me", PatchBody(email: "new@example.com", currentPassword: "pw"));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task NoFields_Returns400()
    {
        var token = await factory.SeedUserWithAccessTokenAsync("patchme_nofields@example.com", "password123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _client.PatchAsync("/v1/me",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task WrongCurrentPassword_Returns401()
    {
        var token = await factory.SeedUserWithAccessTokenAsync("patchme_wrongpw@example.com", "correct-password");
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PatchAsync("/v1/me",
            PatchBody(email: "new@example.com", currentPassword: "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task OAuthUser_Returns403()
    {
        var token = await factory.SeedOAuthUserWithAccessTokenAsync(OAuthProvider.Github, "gh-patch-test");
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PatchAsync("/v1/me",
            PatchBody(email: "new@example.com", currentPassword: "anything"));

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task EmailAlreadyTaken_Returns409()
    {
        await factory.SeedEmailUserAsync("patchme_taken@example.com", "password");
        var token = await factory.SeedUserWithAccessTokenAsync("patchme_changer@example.com", "password123");
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PatchAsync("/v1/me",
            PatchBody(email: "patchme_taken@example.com", currentPassword: "password123"));

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task ValidEmailChange_Returns200_WithNewEmail()
    {
        var token = await factory.SeedUserWithAccessTokenAsync("patchme_oldemail@example.com", "password123");
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PatchAsync("/v1/me",
            PatchBody(email: "patchme_newemail@example.com", currentPassword: "password123"));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.ReadJsonAsync();
        Assert.Equal("patchme_newemail@example.com", json["email"]!.GetValue<string>());
        Assert.False(json["email_verified"]!.GetValue<bool>());
    }

    [Fact]
    public async Task ValidPasswordChange_Returns200()
    {
        var token = await factory.SeedUserWithAccessTokenAsync("patchme_pwchange@example.com", "old-password");
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PatchAsync("/v1/me",
            PatchBody(currentPassword: "old-password", newPassword: "new-password"));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
