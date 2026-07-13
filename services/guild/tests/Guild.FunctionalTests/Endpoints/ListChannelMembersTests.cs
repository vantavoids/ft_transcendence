using System.Net;
using Guild.Domain.Guild;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class ListChannelMembersTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.GetAsync("/v1/guilds/1/channels/1/members");
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task NonMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5301);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "general");

		var stranger = factory.CreateAuthenticatedClient(userId: 5302);
		var resp = await stranger.GetAsync($"/v1/guilds/{guildId}/channels/{channelId}/members");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task ChannelInOtherGuild_Returns_404()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5303);
		var a = await owner.CreateGuildAsync("a");
		var b = await owner.CreateGuildAsync("b");
		var guildA = long.Parse(a.GetProperty("id").GetString()!);
		var guildB = long.Parse(b.GetProperty("id").GetString()!);
		var channelInB = await factory.AddChannelAsync(guildB, categoryId: null, name: "general");

		// the channel exists but belongs to guild B, not the guild in the path
		var resp = await owner.GetAsync($"/v1/guilds/{guildA}/channels/{channelInB}/members");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task NoOverwrites_ReturnsEveryMember()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5304);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(guildId, userId: 5305);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "general");

		var resp = await owner.GetAsync($"/v1/guilds/{guildId}/channels/{channelId}/members");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

		var ids = await ReadUserIdsAsync(resp);
		Assert.Contains("5304", ids);
		Assert.Contains("5305", ids);
	}

	[Fact]
	public async Task MemberDeniedRead_IsExcluded_ButOwnerRemains()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5306);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(guildId, userId: 5307);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "secret");

		// deny READ to member 5307 on this channel
		await factory.AddChannelOverwriteAsync(
			channelId, OverwriteTargetType.Member, targetId: 5307,
			allow: 0L, deny: (long)Permission.ReadMessages);

		var resp = await owner.GetAsync($"/v1/guilds/{guildId}/channels/{channelId}/members");
		var ids = await ReadUserIdsAsync(resp);

		Assert.DoesNotContain("5307", ids);
		// owner short-circuits to all permissions in the resolver, so still a reader
		Assert.Contains("5306", ids);
	}

	private static async Task<List<string>> ReadUserIdsAsync(HttpResponseMessage response)
	{
		var body = await response.ReadJsonAsync();
		var ids = new List<string>();
		foreach (var element in body.GetProperty("user_ids").EnumerateArray())
			ids.Add(element.GetString()!);
		return ids;
	}
}
