using System.Net;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class UserDataExportTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Anonymous_OnInternalGroup_NotUnderV1_RejectsNonPositive()
	{
		var anon = factory.CreateClient();
		// internal group: reachable without a token, but not under /v1 (gateway-facing)
		Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync("/internal/users/1/data-export")).StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync("/v1/users/1/data-export")).StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, (await anon.GetAsync("/internal/users/0/data-export")).StatusCode);
	}

	[Fact]
	public async Task UnknownUser_Returns_EmptyExport()
	{
		var anon = factory.CreateClient();
		var body = await (await anon.GetAsync("/internal/users/888888/data-export")).ReadJsonAsync();

		Assert.Equal("888888", body.GetProperty("user_id").GetString());
		Assert.Empty(body.GetProperty("owned_guilds").EnumerateArray());
		Assert.Empty(body.GetProperty("memberships").EnumerateArray());
	}

	[Fact]
	public async Task Exports_OwnedGuilds_And_Memberships_WithRoleNames()
	{
		// 9701 owns a guild, and is a member of someone else's with an explicit role
		var owner = factory.CreateAuthenticatedClient(userId: 9701);
		await owner.CreateGuildAsync("Owned Server");

		var other = factory.CreateAuthenticatedClient(userId: 9702);
		var otherGuild = await other.CreateGuildAsync("Other Guild");
		var otherGid = long.Parse(otherGuild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(otherGid, userId: 9701);
		var roleId = await factory.SeedRoleAsync(otherGid, "Moderator", position: 1, permissions: 0);
		await factory.AssignRoleDirectAsync(otherGid, userId: 9701, roleId);

		var anon = factory.CreateClient();
		var body = await (await anon.GetAsync("/internal/users/9701/data-export")).ReadJsonAsync();

		Assert.Equal("9701", body.GetProperty("user_id").GetString());

		var owned = body.GetProperty("owned_guilds").EnumerateArray()
			.Select(g => g.GetProperty("name").GetString()).ToList();
		Assert.Contains("Owned Server", owned);

		var otherMembership = body.GetProperty("memberships").EnumerateArray()
			.Single(m => m.GetProperty("guild_name").GetString() == "Other Guild");
		var roles = otherMembership.GetProperty("roles").EnumerateArray()
			.Select(r => r.GetString()).ToList();
		Assert.Contains("Moderator", roles);
	}
}
