using System.Net;
using System.Net.Http.Json;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class UpdateChannelTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Unknown_Channel_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5201);
		var created = await client.CreateGuildAsync("guild");
		var id = created.GetProperty("id").GetString()!;

		var resp = await client.PatchAsJsonAsync(
			$"/v1/guilds/{id}/channels/999999",
			new { name = "x" }, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Renames()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5202);
		var created = await client.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "old");

		var resp = await client.PatchAsJsonAsync(
			$"/v1/guilds/{guildId}/channels/{channelId}",
			new { name = "new-name", position = 5 },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal("new-name", body.GetProperty("name").GetString());
		Assert.Equal(5, body.GetProperty("position").GetInt32());
	}

	[Fact]
	public async Task NonMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5203);
		var stranger = factory.CreateAuthenticatedClient(userId: 5204);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var resp = await stranger.PatchAsJsonAsync(
			$"/v1/guilds/{guildId}/channels/{channelId}",
			new { name = "haxx" }, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task MemberWithoutManageChannels_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5205);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		await factory.AddBareMemberAsync(guildId, userId: 5206);
		var member = factory.CreateAuthenticatedClient(userId: 5206);

		var resp = await member.PatchAsJsonAsync(
			$"/v1/guilds/{guildId}/channels/{channelId}",
			new { name = "haxx" }, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}
}
