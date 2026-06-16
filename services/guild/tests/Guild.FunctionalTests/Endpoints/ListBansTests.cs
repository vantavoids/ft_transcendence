using System.Net;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class ListBansTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.GetAsync("/v1/guilds/1/bans");
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 20_001);
		var resp = await client.GetAsync("/v1/guilds/9999999/bans");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task CallerNotAMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 20_002);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 20_003);
		var resp = await stranger.GetAsync($"/v1/guilds/{id}/bans");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task WithoutBanMembersPerm_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 20_004);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(id, userId: 20_005);

		var bareMember = factory.CreateAuthenticatedClient(userId: 20_005);
		var resp = await bareMember.GetAsync($"/v1/guilds/{id}/bans");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task InvalidLimit_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 20_006);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var tooBig = await owner.GetAsync($"/v1/guilds/{id}/bans?limit=999");
		Assert.Equal(HttpStatusCode.BadRequest, tooBig.StatusCode);

		var tooSmall = await owner.GetAsync($"/v1/guilds/{id}/bans?limit=0");
		Assert.Equal(HttpStatusCode.BadRequest, tooSmall.StatusCode);
	}

	[Fact]
	public async Task InvalidAfterCursor_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 20_007);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.GetAsync($"/v1/guilds/{id}/bans?after=not-a-snowflake");
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task EmptyGuild_Returns_200_EmptyArray()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 20_008);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.GetAsync($"/v1/guilds/{id}/bans");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Empty(body.EnumerateArray());
	}

	[Fact]
	public async Task SeededBans_Returns_200_OrderedByUserIdAscending()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 20_009);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);

		await factory.AddBanAsync(id, userId: 30_002, bannedBy: 20_009, reason: "two");
		await factory.AddBanAsync(id, userId: 30_001, bannedBy: 20_009, reason: "one");
		await factory.AddBanAsync(id, userId: 30_003, bannedBy: 20_009);

		var resp = await owner.GetAsync($"/v1/guilds/{id}/bans");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		var userIds = body.EnumerateArray()
			.Select(b => b.GetProperty("user_id").GetString())
			.ToList();
		Assert.Equal(["30001", "30002", "30003"], userIds);

		var first = body.EnumerateArray().First();
		Assert.Equal("one", first.GetProperty("reason").GetString());
		Assert.Equal("20009", first.GetProperty("banned_by").GetString());
		Assert.True(first.TryGetProperty("banned_at", out _));

		// reason is nullable - third entry was seeded without one
		var third = body.EnumerateArray().Last();
		Assert.Equal(System.Text.Json.JsonValueKind.Null, third.GetProperty("reason").ValueKind);
	}

	[Fact]
	public async Task PaginationCursor_RespectsAfterAndLimit()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 20_010);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);

		for (long uid = 40_001; uid <= 40_005; uid++)
			await factory.AddBanAsync(id, userId: uid, bannedBy: 20_010);

		var page1 = await owner.GetAsync($"/v1/guilds/{id}/bans?limit=2");
		var page1Body = await page1.ReadJsonAsync();
		var page1Ids = page1Body.EnumerateArray()
			.Select(b => b.GetProperty("user_id").GetString())
			.ToList();
		Assert.Equal(["40001", "40002"], page1Ids);

		var page2 = await owner.GetAsync($"/v1/guilds/{id}/bans?limit=2&after=40002");
		var page2Body = await page2.ReadJsonAsync();
		var page2Ids = page2Body.EnumerateArray()
			.Select(b => b.GetProperty("user_id").GetString())
			.ToList();
		Assert.Equal(["40003", "40004"], page2Ids);
	}

	[Fact]
	public async Task BansFromOtherGuild_AreNotIncluded()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 20_011);
		var guildA = await owner.CreateGuildAsync("A");
		var guildB = await owner.CreateGuildAsync("B");
		var idA = long.Parse(guildA.GetProperty("id").GetString()!);
		var idB = long.Parse(guildB.GetProperty("id").GetString()!);

		await factory.AddBanAsync(idA, userId: 50_001, bannedBy: 20_011);
		await factory.AddBanAsync(idB, userId: 50_002, bannedBy: 20_011);

		var resp = await owner.GetAsync($"/v1/guilds/{idA}/bans");
		var body = await resp.ReadJsonAsync();
		var userIds = body.EnumerateArray()
			.Select(b => b.GetProperty("user_id").GetString())
			.ToList();
		Assert.Equal(["50001"], userIds);
	}

}
