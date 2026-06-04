using System.Net;
using System.Net.Http.Json;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class CreateInviteTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.PostAsJsonAsync(
			"/v1/guilds/1/invites", new { }, JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 7301);
		var resp = await client.PostAsJsonAsync(
			"/v1/guilds/9999999/invites", new { }, JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task NotAMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7302);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 7303);
		var resp = await stranger.PostAsJsonAsync(
			$"/v1/guilds/{id}/invites", new { }, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task OwnerHappyPath_Returns_201_WithCode()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7304);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.PostAsJsonAsync(
			$"/v1/guilds/{id}/invites",
			new { max_uses = 10, expires_in_hours = 24 },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		var code = body.GetProperty("code").GetString();
		Assert.False(string.IsNullOrEmpty(code));
		Assert.Equal(id, body.GetProperty("guild_id").GetString());
		Assert.Equal(10, body.GetProperty("max_uses").GetInt32());
		Assert.Equal(0, body.GetProperty("uses").GetInt32());
	}

	[Fact]
	public async Task NoMaxUses_NoExpiry_ReturnsNullValues()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7305);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.PostAsJsonAsync(
			$"/v1/guilds/{id}/invites", new { }, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(System.Text.Json.JsonValueKind.Null, body.GetProperty("max_uses").ValueKind);
		Assert.Equal(System.Text.Json.JsonValueKind.Null, body.GetProperty("expires_at").ValueKind);
	}
}
