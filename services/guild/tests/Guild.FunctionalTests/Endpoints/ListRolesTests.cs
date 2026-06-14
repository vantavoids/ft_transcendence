using System.Net;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class ListRolesTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.GetAsync("/v1/guilds/1/roles");
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 9701);
		var resp = await client.GetAsync("/v1/guilds/9999999/roles");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task NotAMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9702);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 9703);
		var resp = await stranger.GetAsync($"/v1/guilds/{id}/roles");

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_DefaultRoles_OrderedByPositionAscending()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9704);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.GetAsync($"/v1/guilds/{id}/roles");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();

		var names = body.EnumerateArray().Select(r => r.GetProperty("name").GetString()).ToArray();
		Assert.Equal(new[] { "@everyone", "Administrator" }, names);

		Assert.True(body.EnumerateArray().First().GetProperty("is_default").GetBoolean());
		Assert.False(body.EnumerateArray().Last().GetProperty("is_default").GetBoolean());
	}
}
