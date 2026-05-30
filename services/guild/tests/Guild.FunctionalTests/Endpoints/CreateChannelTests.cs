using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class CreateChannelTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.PostAsJsonAsync(
			"/v1/guilds/1/channels", new { name = "general" }, JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task Unknown_Guild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5101);
		var resp = await client.PostAsJsonAsync(
			"/v1/guilds/999999/channels",
			new { name = "general" },
			JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task Non_Member_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5102);
		var stranger = factory.CreateAuthenticatedClient(userId: 5103);
		var created = await owner.CreateGuildAsync("guild");
		var id = created.GetProperty("id").GetString()!;

		var resp = await stranger.PostAsJsonAsync(
			$"/v1/guilds/{id}/channels",
			new { name = "general" }, JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task Member_Without_ManageChannels_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5104);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(guildId, userId: 5105);

		var member = factory.CreateAuthenticatedClient(userId: 5105);
		var resp = await member.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/channels",
			new { name = "general" }, JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task Invalid_Name_Returns_400()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5106);
		var created = await client.CreateGuildAsync("guild");
		var id = created.GetProperty("id").GetString()!;

		var resp = await client.PostAsJsonAsync(
			$"/v1/guilds/{id}/channels",
			new { name = "" }, JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task Invalid_Type_Returns_400()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5107);
		var created = await client.CreateGuildAsync("guild");
		var id = created.GetProperty("id").GetString()!;

		var resp = await client.PostAsJsonAsync(
			$"/v1/guilds/{id}/channels",
			new { name = "general", type = "weird" }, JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_201_AndShape()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5108);
		var created = await client.CreateGuildAsync("guild");
		var guildId = created.GetProperty("id").GetString()!;

		var resp = await client.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/channels",
			new { name = "general", type = "text", position = 0 },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(JsonValueKind.String, body.GetProperty("id").ValueKind);
		Assert.True(long.TryParse(body.GetProperty("id").GetString(), out _));
		Assert.Equal(guildId, body.GetProperty("guild_id").GetString());
		Assert.Equal("general", body.GetProperty("name").GetString());
		Assert.Equal("text", body.GetProperty("type").GetString());
		Assert.Equal(0, body.GetProperty("position").GetInt32());
	}

	[Fact]
	public async Task AnnouncementType_Returns_201()
	{
		// contract: type accepts "text" and "announcement"
		var client = factory.CreateAuthenticatedClient(userId: 5110);
		var created = await client.CreateGuildAsync("guild");
		var id = created.GetProperty("id").GetString()!;

		var resp = await client.PostAsJsonAsync(
			$"/v1/guilds/{id}/channels",
			new { name = "announcements", type = "announcement" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal("announcement", body.GetProperty("type").GetString());
	}

	[Fact]
	public async Task Response_IncludesIsNsfw()
	{
		// contract channel shape includes is_nsfw (defaults to false)
		var client = factory.CreateAuthenticatedClient(userId: 5111);
		var created = await client.CreateGuildAsync("guild");
		var id = created.GetProperty("id").GetString()!;

		var resp = await client.PostAsJsonAsync(
			$"/v1/guilds/{id}/channels",
			new { name = "general", type = "text" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.False(body.GetProperty("is_nsfw").GetBoolean());
	}

	[Fact]
	public async Task Response_IncludesSlowmodeSeconds()
	{
		// contract channel shape includes slowmode_seconds (defaults to 0)
		var client = factory.CreateAuthenticatedClient(userId: 5112);
		var created = await client.CreateGuildAsync("guild");
		var id = created.GetProperty("id").GetString()!;

		var resp = await client.PostAsJsonAsync(
			$"/v1/guilds/{id}/channels",
			new { name = "general", type = "text" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(0, body.GetProperty("slowmode_seconds").GetInt32());
	}

	[Fact]
	public async Task WithoutPosition_AutoAppendsPerCategory()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5109);
		var created = await client.CreateGuildAsync("guild");
		var guildId = created.GetProperty("id").GetString()!;

		var r1 = await client.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/channels",
			new { name = "first", type = "text" },
			JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.Created, r1.StatusCode);
		var b1 = await r1.ReadJsonAsync();

		var r2 = await client.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/channels",
			new { name = "second", type = "text" },
			JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.Created, r2.StatusCode);
		var b2 = await r2.ReadJsonAsync();

		Assert.Equal(
			b1.GetProperty("position").GetInt32() + 1,
			b2.GetProperty("position").GetInt32());
	}
}
