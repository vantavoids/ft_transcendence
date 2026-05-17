using System.Net;
using Auth.FunctionalTests.Infrastructure;
using Xunit;

namespace Auth.FunctionalTests.Endpoints;

public sealed class OAuthLoginTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Theory]
    [InlineData("fortytwo")]
    [InlineData("google")]
    [InlineData("github")]
    public async Task ValidProvider_Returns302_WithLocationAndStateCookie(string provider)
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var resp = await client.GetAsync($"/v1/oauth/{provider}");

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.NotNull(resp.Headers.Location);
        Assert.Contains("state=", resp.Headers.Location!.Query);

        var setCookie = resp.Headers.GetValues("Set-Cookie").FirstOrDefault(c => c.StartsWith("oauth_state="));
        Assert.NotNull(setCookie);
        Assert.Contains("HttpOnly", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownProvider_Returns400()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var resp = await client.GetAsync("/v1/oauth/twitter");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
