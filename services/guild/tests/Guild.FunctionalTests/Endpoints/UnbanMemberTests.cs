using System.Net;
using System.Net.Http.Json;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class UnbanMemberTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.DeleteAsync("/v1/guilds/1/bans/2");
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 70_001);
		var resp = await client.DeleteAsync("/v1/guilds/9999999/bans/70_002");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task CallerNotAMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 70_003);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 70_004);
		var resp = await stranger.DeleteAsync($"/v1/guilds/{id}/bans/{70_005}");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task WithoutBanMembersPerm_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 70_006);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(id, userId: 70_007);
		await factory.AddBanAsync(id, userId: 70_008, bannedBy: 70_006);

		var bareMember = factory.CreateAuthenticatedClient(userId: 70_007);
		var resp = await bareMember.DeleteAsync($"/v1/guilds/{id}/bans/{70_008}");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task BanNotFound_Returns_404()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 70_009);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.DeleteAsync($"/v1/guilds/{id}/bans/{70_010}");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_204_AndAllowsRejoining()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 70_011);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);

		// pre-emptive ban, then unban
		var ban = await owner.PostAsJsonAsync($"/v1/guilds/{id}/bans/{70_012}", new { });
		Assert.Equal(HttpStatusCode.NoContent, ban.StatusCode);

		var unban = await owner.DeleteAsync($"/v1/guilds/{id}/bans/{70_012}");
		Assert.Equal(HttpStatusCode.NoContent, unban.StatusCode);

		// list confirms the ban is gone
		var bans = await owner.GetAsync($"/v1/guilds/{id}/bans");
		var userIds = (await bans.ReadJsonAsync()).EnumerateArray()
			.Select(b => b.GetProperty("user_id").GetString())
			.ToList();
		Assert.DoesNotContain("70012", userIds);

		// second unban now 404s
		var second = await owner.DeleteAsync($"/v1/guilds/{id}/bans/{70_012}");
		Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
	}
}
