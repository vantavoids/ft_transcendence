using System.Net;
using System.Net.Http.Headers;
using Auth.FunctionalTests.Infrastructure;
using Xunit;

namespace Auth.FunctionalTests.Endpoints;

public sealed class DeleteMeEndpointTests(AuthApiFactory factory)
    : IClassFixture<AuthApiFactory>
{
    [Fact]
    public async Task NoToken_Returns401()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var resp = await client.DeleteAsync("/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task ValidUser_Returns204()
    {
        var token = await factory.SeedUserWithAccessTokenAsync("deleteme_valid@example.com", "password123");
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.DeleteAsync("/v1/me");

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task ValidUser_SubsequentGetMe_Returns401()
    {
        var token = await factory.SeedUserWithAccessTokenAsync("deleteme_subsequent@example.com", "password123");
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.DeleteAsync("/v1/me");
        var resp = await client.GetAsync("/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task UserOwnsGuilds_Returns409()
    {
        var token = await factory.SeedUserWithAccessTokenAsync("deleteme_guildowner@example.com", "password123");
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        factory.GuildClient.OwnedGuildsCount = 1;
        try
        {
            var resp = await client.DeleteAsync("/v1/me");
            Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        }
        finally
        {
            factory.GuildClient.OwnedGuildsCount = 0;
        }
    }
}
