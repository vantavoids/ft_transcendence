using System.Net;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class ListMyGuildsTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.GetAsync("/v1/guilds/me");
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task NoMemberships_Returns_EmptyList()
	{
		var client = factory.CreateAuthenticatedClient(userId: 7001);
		var resp = await client.GetAsync("/v1/guilds/me");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(0, body.GetArrayLength());
	}

	[Fact]
	public async Task Returns_Only_Guilds_Caller_Belongs_To()
	{
		var caller = factory.CreateAuthenticatedClient(userId: 7002);
		var other = factory.CreateAuthenticatedClient(userId: 7003);

		var a = await caller.CreateGuildAsync("alpha");
		var b = await caller.CreateGuildAsync("beta");
		await other.CreateGuildAsync("not mine");

		var resp = await caller.GetAsync("/v1/guilds/me");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(2, body.GetArrayLength());

		var ids = new HashSet<string>();
		foreach (var g in body.EnumerateArray())
			ids.Add(g.GetProperty("id").GetString()!);
		Assert.Contains(a.GetProperty("id").GetString()!, ids);
		Assert.Contains(b.GetProperty("id").GetString()!, ids);
	}

	[Fact]
	public async Task Entry_Carries_Summary_Shape()
	{
		var client = factory.CreateAuthenticatedClient(userId: 7004);
		var created = await client.CreateGuildAsync("gamma", iconUrl: "https://cdn/icon.png");
		var guildId = created.GetProperty("id").GetString()!;

		var resp = await client.GetAsync("/v1/guilds/me");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		var entry = body[0];

		Assert.Equal(guildId, entry.GetProperty("id").GetString());
		Assert.Equal("gamma", entry.GetProperty("name").GetString());
		Assert.Equal("https://cdn/icon.png", entry.GetProperty("icon_url").GetString());
		Assert.Equal("7004", entry.GetProperty("owner_id").GetString());
		Assert.Equal(1, entry.GetProperty("member_count").GetInt32());
		Assert.True(entry.TryGetProperty("joined_at", out _));
	}
}
