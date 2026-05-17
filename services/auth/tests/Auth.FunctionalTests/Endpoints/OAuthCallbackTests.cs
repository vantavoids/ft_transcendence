using System.Net;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Auth.FunctionalTests.Infrastructure;
using Xunit;

namespace Auth.FunctionalTests.Endpoints;

public sealed class OAuthCallbackTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private HttpClient CreateClient() =>
        factory.CreateClient(new() { AllowAutoRedirect = false });

    private async Task<HttpResponseMessage> DoFullCallback(string provider = "fortytwo", string code = "code-xyz")
    {
        var client = CreateClient();

        var initResp = await client.GetAsync($"/v1/oauth/{provider}");
        var stateCookie = initResp.Headers
            .GetValues("Set-Cookie")
            .First(c => c.StartsWith("oauth_state="));

        var stateValue = stateCookie.Split(';')[0].Split('=', 2)[1];

        // 2. callback avec le même cookie et le même state
        var req = new HttpRequestMessage(HttpMethod.Get,
            $"/v1/oauth/{provider}/callback?code={code}&state={stateValue}");
        req.Headers.Add("Cookie", $"oauth_state={stateValue}");

        return await client.SendAsync(req);
    }

    [Fact]
    public async Task ValidCallback_Returns302_WithAccessTokenAndRefreshCookie()
    {
        var resp = await DoFullCallback();

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);

        var location = resp.Headers.Location!.ToString();
        Assert.Contains("access_token=", location);

        var refreshCookie = resp.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(c => c.StartsWith("refresh_token="));
        Assert.NotNull(refreshCookie);
        Assert.Contains("HttpOnly", refreshCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NewUser_RedirectContainsNewUserFlag()
    {
        factory.OAuthClient.SetupSuccess(new OAuthUserInfo("new-user-42", "newuser@example.com", true));

        var resp = await DoFullCallback();

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("new_user=true", resp.Headers.Location!.ToString());
    }

    [Fact]
    public async Task ExistingUser_RedirectDoesNotContainNewUserFlag()
    {
        factory.OAuthClient.SetupSuccess(new OAuthUserInfo("existing-42", "existing@example.com", true));
        await DoFullCallback();

        var resp = await DoFullCallback();
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.DoesNotContain("new_user=true", resp.Headers.Location!.ToString());
    }

    [Fact]
    public async Task StateMismatch_Returns400()
    {
        var client = CreateClient();

        var req = new HttpRequestMessage(HttpMethod.Get,
            "/v1/oauth/fortytwo/callback?code=xyz&state=wrong-state");
        req.Headers.Add("Cookie", "oauth_state=correct-state");

        var resp = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task MissingStateCookie_Returns400()
    {
        var client = CreateClient();

        var resp = await client.GetAsync(
            "/v1/oauth/fortytwo/callback?code=xyz&state=any-state");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ProviderError_Returns400()
    {
        factory.OAuthClient.SetupFailure(AuthFailures.OAuthProviderError);

        var resp = await DoFullCallback();

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UnknownProvider_Returns400()
    {
        var client = CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get,
            "/v1/oauth/twitter/callback?code=xyz&state=state");
        req.Headers.Add("Cookie", "oauth_state=state");

        var resp = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
