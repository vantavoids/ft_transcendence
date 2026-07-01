using System.Net;
using System.Net.Http.Json;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class ListCategoriesTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.GetAsync("/v1/guilds/1/categories");
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task Unknown_Guild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 6001);
		var resp = await client.GetAsync("/v1/guilds/999999/categories");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task Non_Member_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 6002);
		var stranger = factory.CreateAuthenticatedClient(userId: 6003);
		var created = await owner.CreateGuildAsync("guild");
		var id = created.GetProperty("id").GetString()!;

		var resp = await stranger.GetAsync($"/v1/guilds/{id}/categories");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task Member_Sees_EmptyList_Initially()
	{
		var client = factory.CreateAuthenticatedClient(userId: 6004);
		var created = await client.CreateGuildAsync("guild");
		var id = created.GetProperty("id").GetString()!;

		var resp = await client.GetAsync($"/v1/guilds/{id}/categories");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(0, body.GetArrayLength());
	}

	[Fact]
	public async Task Member_Sees_Categories_OrderedByPosition()
	{
		var client = factory.CreateAuthenticatedClient(userId: 6005);
		var created = await client.CreateGuildAsync("guild");
		var id = created.GetProperty("id").GetString()!;

		await CreateCategoryAsync(client, id, "second", position: 1);
		await CreateCategoryAsync(client, id, "first", position: 0);

		var resp = await client.GetAsync($"/v1/guilds/{id}/categories");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(2, body.GetArrayLength());
		Assert.Equal("first", body[0].GetProperty("name").GetString());
		Assert.Equal("second", body[1].GetProperty("name").GetString());
		// snowflake ids are emitted as quoted strings and guild_id echoes the route
		Assert.Equal(id, body[0].GetProperty("guild_id").GetString());
		Assert.Equal(0, body[0].GetProperty("position").GetInt32());
	}

	private static async Task CreateCategoryAsync(HttpClient client, string guildId, string name, int position)
	{
		var resp = await client.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/categories",
			new { name, position },
			JsonOptions.SnakeCase);
		resp.EnsureSuccessStatusCode();
	}
}
