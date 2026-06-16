using System.Net;
using System.Net.Http.Json;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class JoinGuildTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.PostAsJsonAsync(
			"/v1/guilds/1/join", new { invite_code = "x" }, JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuildAndUnknownCode_Returns_400()
	{
		// the contract lists only 400/409 for this endpoint; "unknown guild" is
		// indistinguishable from "unknown code" since both reduce to "no usable
		// invite". the join-by-code flow under /invites/{code}/join returns 404
		// instead, see JoinByCodeTests
		var client = factory.CreateAuthenticatedClient(userId: 7101);
		var resp = await client.PostAsJsonAsync(
			"/v1/guilds/9999999/join", new { invite_code = "x" }, JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task MissingInviteCode_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7102);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 7103);
		var resp = await stranger.PostAsJsonAsync(
			$"/v1/guilds/{id}/join", new { invite_code = (string?)null }, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownCode_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7104);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 7105);
		var resp = await stranger.PostAsJsonAsync(
			$"/v1/guilds/{id}/join", new { invite_code = "no-such-code" }, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task CodeFromDifferentGuild_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7106);
		var g1 = await owner.CreateGuildAsync("g1");
		var g2 = await owner.CreateGuildAsync("g2");
		var g1Id = long.Parse(g1.GetProperty("id").GetString()!);
		var g2Id = long.Parse(g2.GetProperty("id").GetString()!);

		await factory.AddInviteAsync(g1Id, "code-from-g1", createdBy: 7106);

		var stranger = factory.CreateAuthenticatedClient(userId: 7107);
		var resp = await stranger.PostAsJsonAsync(
			$"/v1/guilds/{g2Id}/join", new { invite_code = "code-from-g1" }, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task AlreadyMember_Returns_409()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7108);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddInviteAsync(id, "owner-code", createdBy: 7108);

		var resp = await owner.PostAsJsonAsync(
			$"/v1/guilds/{id}/join", new { invite_code = "owner-code" }, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_200_WithGuildBody()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7109);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddInviteAsync(id, "join-me", createdBy: 7109);

		var stranger = factory.CreateAuthenticatedClient(userId: 7110);
		var resp = await stranger.PostAsJsonAsync(
			$"/v1/guilds/{id}/join", new { invite_code = "join-me" }, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(id.ToString(), body.GetProperty("id").GetString());
	}

	[Fact]
	public async Task BannedUser_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7111);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddInviteAsync(id, "ban-block", createdBy: 7111);
		await factory.AddBanAsync(id, userId: 7112, bannedBy: 7111);

		var banned = factory.CreateAuthenticatedClient(userId: 7112);
		var resp = await banned.PostAsJsonAsync(
			$"/v1/guilds/{id}/join", new { invite_code = "ban-block" }, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}
}
