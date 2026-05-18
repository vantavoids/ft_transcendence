using System.Net;
using Guild.Domain.Guild;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class GetChannelMembershipTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task NoToken_StillOk_BecauseEndpointAllowsAnonymous()
	{
		// owner creates a guild + channel
		var owner = factory.CreateAuthenticatedClient(userId: 5601);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		// anonymous client - the Chat Service mimics this with internal traffic
		var anon = factory.CreateClient();
		var resp = await anon.GetAsync($"/v1/channels/{channelId}/membership?user_id=5601");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
	}

	[Fact]
	public async Task Unknown_Channel_Returns_404()
	{
		var anon = factory.CreateClient();
		var resp = await anon.GetAsync("/v1/channels/999999/membership?user_id=1");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task Missing_UserId_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5602);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var anon = factory.CreateClient();
		var resp = await anon.GetAsync($"/v1/channels/{channelId}/membership");
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task NonMember_Returns_IsMemberFalse_WithGuildIdAndZeroPermissions()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5603);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var anon = factory.CreateClient();
		var resp = await anon.GetAsync($"/v1/channels/{channelId}/membership?user_id=99999");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.False(body.GetProperty("is_member").GetBoolean());
		Assert.Equal(guildId.ToString(), body.GetProperty("guild_id").GetString());
		Assert.Equal(0L, body.GetProperty("permissions").GetInt64());
	}

	[Fact]
	public async Task Owner_Returns_AllBitsSet()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5604);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var anon = factory.CreateClient();
		var resp = await anon.GetAsync($"/v1/channels/{channelId}/membership?user_id=5604");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.True(body.GetProperty("is_member").GetBoolean());
		Assert.Equal(guildId.ToString(), body.GetProperty("guild_id").GetString());
		Assert.Equal(~0L, body.GetProperty("permissions").GetInt64());
	}

	[Fact]
	public async Task MemberWithEveryoneOnly_GetsEveryoneMask()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5605);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");
		await factory.AddBareMemberAsync(guildId, userId: 5606);

		var anon = factory.CreateClient();
		var resp = await anon.GetAsync($"/v1/channels/{channelId}/membership?user_id=5606");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.True(body.GetProperty("is_member").GetBoolean());
		// 515 = SendMessages|ReadMessages|CreateInvite (the @everyone defaults)
		Assert.Equal(515L, body.GetProperty("permissions").GetInt64());
	}

	[Fact]
	public async Task MemberWithChannelMemberOverwrite_HasOverrideApplied()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5607);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");
		await factory.AddBareMemberAsync(guildId, userId: 5608);

		// member overwrite denies ReadMessages (bit 2)
		await factory.AddChannelOverwriteAsync(
			channelId, OverwriteTargetType.Member, targetId: 5608,
			allow: 0L, deny: (long)Permission.ReadMessages);

		var anon = factory.CreateClient();
		var resp = await anon.GetAsync($"/v1/channels/{channelId}/membership?user_id=5608");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		var perms = body.GetProperty("permissions").GetInt64();
		Assert.Equal(0L, perms & (long)Permission.ReadMessages);
	}

	[Fact]
	public async Task Response_HasOnlyExpectedKeys()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5609);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var anon = factory.CreateClient();
		var resp = await anon.GetAsync($"/v1/channels/{channelId}/membership?user_id=5609");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		var keys = new HashSet<string>();
		foreach (var prop in body.EnumerateObject()) keys.Add(prop.Name);
		Assert.Equal(new HashSet<string> { "is_member", "guild_id", "permissions" }, keys);
	}
}
