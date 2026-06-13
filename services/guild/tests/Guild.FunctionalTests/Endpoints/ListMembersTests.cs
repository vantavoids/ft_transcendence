using System.Net;
using Guild.Domain.Guild;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class ListMembersTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.GetAsync("/v1/guilds/1/members");
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 9101);
		var resp = await client.GetAsync("/v1/guilds/9999999/members");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task NotAMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9102);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 9103);
		var resp = await stranger.GetAsync($"/v1/guilds/{id}/members");

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_OwnerAndExtraMembers_OrderedByUserIdAscending()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9104);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(id, userId: 9106);
		await factory.AddBareMemberAsync(id, userId: 9105);

		var resp = await owner.GetAsync($"/v1/guilds/{id}/members");

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		var ids = body.EnumerateArray().Select(e => e.GetProperty("user_id").GetString()).ToArray();
		Assert.Equal(new[] { "9104", "9105", "9106" }, ids);
	}

	[Fact]
	public async Task Cursor_Filters_StrictlyGreater_AndLimit_Caps()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9107);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(id, userId: 9108);
		await factory.AddBareMemberAsync(id, userId: 9109);
		await factory.AddBareMemberAsync(id, userId: 9110);

		var resp = await owner.GetAsync($"/v1/guilds/{id}/members?after=9108&limit=2");

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var ids = (await resp.ReadJsonAsync())
			.EnumerateArray().Select(e => e.GetProperty("user_id").GetString()).ToArray();
		Assert.Equal(new[] { "9109", "9110" }, ids);
	}

	[Fact]
	public async Task InvalidAfter_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9111);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.GetAsync($"/v1/guilds/{id}/members?after=notanumber");

		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task InvalidLimit_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9112);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var tooBig = await owner.GetAsync($"/v1/guilds/{id}/members?limit=101");
		Assert.Equal(HttpStatusCode.BadRequest, tooBig.StatusCode);

		var nonPositive = await owner.GetAsync($"/v1/guilds/{id}/members?limit=0");
		Assert.Equal(HttpStatusCode.BadRequest, nonPositive.StatusCode);
	}

	[Fact]
	public async Task MemberWithExplicitRole_HasRoleIdInResponse_OwnerHasAdminRole()
	{
		// AddMemberWithPermissionsAsync seeds a custom role assigned to the user;
		// the response should list its snowflake id under roles[]. owner has the
		// admin role auto-assigned at guild creation; @everyone is filtered out.
		var owner = factory.CreateAuthenticatedClient(userId: 9113);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddMemberWithPermissionsAsync(id, userId: 9114, permissions: (long)Permission.KickMembers);

		var resp = await owner.GetAsync($"/v1/guilds/{id}/members");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();

		var seeded = body.EnumerateArray().Single(m => m.GetProperty("user_id").GetString() == "9114");
		Assert.Single(seeded.GetProperty("roles").EnumerateArray());

		var ownerRow = body.EnumerateArray().Single(m => m.GetProperty("user_id").GetString() == "9113");
		Assert.Single(ownerRow.GetProperty("roles").EnumerateArray());
	}
}
