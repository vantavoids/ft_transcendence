using System.Net;
using System.Net.Http.Json;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class UpdateCategoryTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.PatchAsJsonAsync(
			"/v1/guilds/1/categories/1",
			new { name = "x" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task Unknown_Guild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5001);
		var resp = await client.PatchAsJsonAsync(
			"/v1/guilds/999999/categories/1",
			new { name = "x" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task Unknown_Category_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5002);
		var created = await client.CreateGuildAsync("guild");
		var guildId = created.GetProperty("id").GetString()!;

		var resp = await client.PatchAsJsonAsync(
			$"/v1/guilds/{guildId}/categories/999999",
			new { name = "x" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task Non_Member_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5003);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = created.GetProperty("id").GetString()!;

		var catResp = await owner.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/categories",
			new { name = "Cat" },
			JsonOptions.SnakeCase);
		var cat = await catResp.ReadJsonAsync();
		var catId = cat.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 5004);
		var resp = await stranger.PatchAsJsonAsync(
			$"/v1/guilds/{guildId}/categories/{catId}",
			new { name = "hacked" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task Member_Without_Permission_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5005);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);

		var catResp = await owner.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/categories",
			new { name = "Cat" },
			JsonOptions.SnakeCase);
		var cat = await catResp.ReadJsonAsync();
		var catId = cat.GetProperty("id").GetString()!;

		await factory.AddBareMemberAsync(guildId, userId: 5006);

		var member = factory.CreateAuthenticatedClient(userId: 5006);
		var resp = await member.PatchAsJsonAsync(
			$"/v1/guilds/{guildId}/categories/{catId}",
			new { name = "no" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task Invalid_Name_Returns_400()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5007);
		var created = await client.CreateGuildAsync("guild");
		var guildId = created.GetProperty("id").GetString()!;

		var catResp = await client.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/categories",
			new { name = "Cat" },
			JsonOptions.SnakeCase);
		var cat = await catResp.ReadJsonAsync();
		var catId = cat.GetProperty("id").GetString()!;

		var resp = await client.PatchAsJsonAsync(
			$"/v1/guilds/{guildId}/categories/{catId}",
			new { name = "" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task Rename_Only_Returns_200()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5008);
		var created = await client.CreateGuildAsync("guild");
		var guildId = created.GetProperty("id").GetString()!;

		var catResp = await client.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/categories",
			new { name = "Original", position = 3 },
			JsonOptions.SnakeCase);
		var cat = await catResp.ReadJsonAsync();
		var catId = cat.GetProperty("id").GetString()!;

		var resp = await client.PatchAsJsonAsync(
			$"/v1/guilds/{guildId}/categories/{catId}",
			new { name = "Renamed" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal("Renamed", body.GetProperty("name").GetString());
		Assert.Equal(3, body.GetProperty("position").GetInt32());
	}

	[Fact]
	public async Task Reposition_Only_Returns_200()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5009);
		var created = await client.CreateGuildAsync("guild");
		var guildId = created.GetProperty("id").GetString()!;

		var catResp = await client.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/categories",
			new { name = "Original", position = 0 },
			JsonOptions.SnakeCase);
		var cat = await catResp.ReadJsonAsync();
		var catId = cat.GetProperty("id").GetString()!;

		var resp = await client.PatchAsJsonAsync(
			$"/v1/guilds/{guildId}/categories/{catId}",
			new { position = 12 },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal("Original", body.GetProperty("name").GetString());
		Assert.Equal(12, body.GetProperty("position").GetInt32());
	}

	[Fact]
	public async Task Both_Fields_Returns_200()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5010);
		var created = await client.CreateGuildAsync("guild");
		var guildId = created.GetProperty("id").GetString()!;

		var catResp = await client.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/categories",
			new { name = "Original", position = 0 },
			JsonOptions.SnakeCase);
		var cat = await catResp.ReadJsonAsync();
		var catId = cat.GetProperty("id").GetString()!;

		var resp = await client.PatchAsJsonAsync(
			$"/v1/guilds/{guildId}/categories/{catId}",
			new { name = "Renamed", position = 7 },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal("Renamed", body.GetProperty("name").GetString());
		Assert.Equal(7, body.GetProperty("position").GetInt32());
	}
}
