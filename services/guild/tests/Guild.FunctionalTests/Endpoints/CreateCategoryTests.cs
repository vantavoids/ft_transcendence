using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class CreateCategoryTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.PostAsJsonAsync(
			"/v1/guilds/1/categories",
			new { name = "Text" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task Unknown_Guild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 4001);
		var resp = await client.PostAsJsonAsync(
			"/v1/guilds/999999/categories",
			new { name = "Text" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task Non_Member_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 4002);
		var stranger = factory.CreateAuthenticatedClient(userId: 4003);

		var created = await owner.CreateGuildAsync("locked");
		var id = created.GetProperty("id").GetString()!;

		var resp = await stranger.PostAsJsonAsync(
			$"/v1/guilds/{id}/categories",
			new { name = "Text" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.True(body.TryGetProperty("error", out _));
	}

	[Fact]
	public async Task Member_Without_ManageChannels_Returns_403()
	{
		// owner creates the guild, then we ask the same factory's in-memory DB
		// to manually attach a second member with only @everyone. since the
		// endpoint to add a member doesn't exist yet, we inject directly via DI
		var owner = factory.CreateAuthenticatedClient(userId: 4004);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);

		await factory.AddBareMemberAsync(guildId, userId: 4005);

		var member = factory.CreateAuthenticatedClient(userId: 4005);
		var resp = await member.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/categories",
			new { name = "Text" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task Invalid_Name_Returns_400()
	{
		var client = factory.CreateAuthenticatedClient(userId: 4006);
		var created = await client.CreateGuildAsync("guild");
		var id = created.GetProperty("id").GetString()!;

		var resp = await client.PostAsJsonAsync(
			$"/v1/guilds/{id}/categories",
			new { name = "" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_201_AndContractShape()
	{
		var client = factory.CreateAuthenticatedClient(userId: 4007);
		var created = await client.CreateGuildAsync("guild");
		var guildId = created.GetProperty("id").GetString()!;

		var resp = await client.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/categories",
			new { name = "Text Channels", position = 0 },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

		var root = await resp.ReadJsonAsync();
		Assert.Equal(JsonValueKind.String, root.GetProperty("id").ValueKind);
		Assert.True(long.TryParse(root.GetProperty("id").GetString(), out _));
		Assert.Equal(guildId, root.GetProperty("guild_id").GetString());
		Assert.Equal("Text Channels", root.GetProperty("name").GetString());
		Assert.Equal(0, root.GetProperty("position").GetInt32());

		// no extra keys beyond {id, guild_id, name, position}
		var keys = new HashSet<string>();
		foreach (var prop in root.EnumerateObject())
			keys.Add(prop.Name);
		Assert.Equal(new HashSet<string> { "id", "guild_id", "name", "position" }, keys);
	}

	[Fact]
	public async Task Without_Position_AutoAppends_Increments()
	{
		var client = factory.CreateAuthenticatedClient(userId: 4008);
		var created = await client.CreateGuildAsync("guild");
		var guildId = created.GetProperty("id").GetString()!;

		var firstResp = await client.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/categories",
			new { name = "First" },
			JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.Created, firstResp.StatusCode);
		var first = await firstResp.ReadJsonAsync();
		var firstPos = first.GetProperty("position").GetInt32();

		var secondResp = await client.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/categories",
			new { name = "Second" },
			JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.Created, secondResp.StatusCode);
		var second = await secondResp.ReadJsonAsync();
		var secondPos = second.GetProperty("position").GetInt32();

		Assert.Equal(firstPos + 1, secondPos);
	}
}
