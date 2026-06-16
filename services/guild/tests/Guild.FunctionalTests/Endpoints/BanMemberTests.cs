using System.Net;
using System.Net.Http.Json;
using Guild.Domain.Guild;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class BanMemberTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.PostAsJsonAsync("/v1/guilds/1/bans/2", new { });
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 60_001);
		var resp = await client.PostAsJsonAsync("/v1/guilds/9999999/bans/60_002", new { });
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task CallerNotAMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 60_003);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 60_004);
		var resp = await stranger.PostAsJsonAsync($"/v1/guilds/{id}/bans/{60_005}", new { });
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task WithoutBanMembersPerm_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 60_006);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(id, userId: 60_007);
		await factory.AddBareMemberAsync(id, userId: 60_008);

		var bareMember = factory.CreateAuthenticatedClient(userId: 60_007);
		var resp = await bareMember.PostAsJsonAsync($"/v1/guilds/{id}/bans/{60_008}", new { });
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task SelfBan_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 60_009);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.PostAsJsonAsync($"/v1/guilds/{id}/bans/{60_009}", new { });
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task BanningOwner_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 60_010);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddMemberWithPermissionsAsync(id, userId: 60_011, permissions: (long)Permission.BanMembers);

		var mod = factory.CreateAuthenticatedClient(userId: 60_011);
		var resp = await mod.PostAsJsonAsync($"/v1/guilds/{id}/bans/{60_010}", new { });
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task TargetOutranksCaller_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 60_012);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);

		// caller is at position 99 with BanMembers; target gets a custom role at position 200
		await factory.AddMemberWithPermissionsAsync(id, userId: 60_013, permissions: (long)Permission.BanMembers);

		// target: a member with a manually-seeded role above the caller's position
		await factory.AddBareMemberAsync(id, userId: 60_014);
		var highRoleId = await factory.SeedRoleAsync(id, "Untouchable", position: 200, permissions: 0L);
		await factory.AssignRoleDirectAsync(id, userId: 60_014, roleId: highRoleId);

		var mod = factory.CreateAuthenticatedClient(userId: 60_013);
		var resp = await mod.PostAsJsonAsync($"/v1/guilds/{id}/bans/{60_014}", new { });
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task ReasonTooLong_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 60_015);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;
		await factory.AddBareMemberAsync(long.Parse(id), userId: 60_016);

		var resp = await owner.PostAsJsonAsync($"/v1/guilds/{id}/bans/{60_016}", new
		{
			reason = new string('x', GuildBan.MaxReasonLen + 1),
		});
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task AlreadyBanned_Returns_409()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 60_017);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBanAsync(id, userId: 60_018, bannedBy: 60_017);

		var resp = await owner.PostAsJsonAsync($"/v1/guilds/{id}/bans/{60_018}", new { });
		Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_BansMember_Returns_204_AndKicksThem()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 60_019);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(id, userId: 60_020);

		var ban = await owner.PostAsJsonAsync($"/v1/guilds/{id}/bans/{60_020}", new
		{
			reason = "il a segfault",
		});
		Assert.Equal(HttpStatusCode.NoContent, ban.StatusCode);

		// member is gone: list of members no longer contains them
		var members = await owner.GetAsync($"/v1/guilds/{id}/members");
		var memberIds = (await members.ReadJsonAsync()).EnumerateArray()
			.Select(m => m.GetProperty("user_id").GetString())
			.ToList();
		Assert.DoesNotContain("60020", memberIds);

		// ban is recorded
		var bans = await owner.GetAsync($"/v1/guilds/{id}/bans");
		var firstBan = (await bans.ReadJsonAsync()).EnumerateArray().First();
		Assert.Equal("60020", firstBan.GetProperty("user_id").GetString());
		Assert.Equal("60019", firstBan.GetProperty("banned_by").GetString());
		Assert.Equal("il a segfault", firstBan.GetProperty("reason").GetString());
	}

	[Fact]
	public async Task HappyPath_PreEmptiveBan_Returns_204()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 60_021);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);

		// target has never been a member
		var ban = await owner.PostAsJsonAsync($"/v1/guilds/{id}/bans/{60_022}", new { });
		Assert.Equal(HttpStatusCode.NoContent, ban.StatusCode);

		var bans = await owner.GetAsync($"/v1/guilds/{id}/bans");
		var body = await bans.ReadJsonAsync();
		var userIds = body.EnumerateArray()
			.Select(b => b.GetProperty("user_id").GetString())
			.ToList();
		Assert.Contains("60022", userIds);
	}

	[Fact]
	public async Task NullBody_IsAccepted()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 60_023);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;
		await factory.AddBareMemberAsync(long.Parse(id), userId: 60_024);

		// no body at all - server should accept and store reason=null
		var resp = await owner.PostAsync($"/v1/guilds/{id}/bans/{60_024}", content: null);
		Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

		var bans = await owner.GetAsync($"/v1/guilds/{id}/bans");
		var first = (await bans.ReadJsonAsync()).EnumerateArray().First();
		Assert.Equal(System.Text.Json.JsonValueKind.Null, first.GetProperty("reason").ValueKind);
	}
}
