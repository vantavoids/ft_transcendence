using System.Net;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class DeleteChannelTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Unknown_Channel_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5301);
		var created = await client.CreateGuildAsync("guild");
		var id = created.GetProperty("id").GetString()!;

		var resp = await client.DeleteAsync($"/v1/guilds/{id}/channels/999999");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_204()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5302);
		var created = await client.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var resp = await client.DeleteAsync($"/v1/guilds/{guildId}/channels/{channelId}");
		Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

		// confirm gone
		var list = await client.GetAsync($"/v1/guilds/{guildId}/channels");
		var body = await list.ReadJsonAsync();
		Assert.Equal(0, body.GetArrayLength());
	}

	[Fact]
	public async Task NonMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5303);
		var stranger = factory.CreateAuthenticatedClient(userId: 5304);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var resp = await stranger.DeleteAsync($"/v1/guilds/{guildId}/channels/{channelId}");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}
}
