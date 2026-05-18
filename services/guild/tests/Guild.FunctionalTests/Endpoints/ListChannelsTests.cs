using System.Net;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class ListChannelsTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.GetAsync("/v1/guilds/1/channels");
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task Unknown_Guild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5001);
		var resp = await client.GetAsync("/v1/guilds/999999/channels");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task Non_Member_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5002);
		var stranger = factory.CreateAuthenticatedClient(userId: 5003);
		var created = await owner.CreateGuildAsync("guild");
		var id = created.GetProperty("id").GetString()!;

		var resp = await stranger.GetAsync($"/v1/guilds/{id}/channels");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task Member_Sees_EmptyList_Initially()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5004);
		var created = await client.CreateGuildAsync("guild");
		var id = created.GetProperty("id").GetString()!;

		var resp = await client.GetAsync($"/v1/guilds/{id}/channels");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(0, body.GetArrayLength());
	}
}
