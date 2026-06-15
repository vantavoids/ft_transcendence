using System.Net;
using System.Net.Http.Json;
using Guild.Domain.Guild;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class AssignRoleTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.PutAsync("/v1/guilds/1/members/2/roles/3", content: null);
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 10_001);
		var resp = await client.PutAsync("/v1/guilds/9999999/members/2/roles/3", content: null);
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task CallerNotAMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_002);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 10_003);
		var resp = await stranger.PutAsync($"/v1/guilds/{id}/members/{10_002}/roles/1", content: null);
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task WithoutManageRolesPerm_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_004);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(id, userId: 10_005);
		await factory.AddBareMemberAsync(id, userId: 10_006);
		var roleId = await factory.SeedRoleAsync(id, "Mod", position: 50, permissions: 0L);

		var bareMember = factory.CreateAuthenticatedClient(userId: 10_005);
		var resp = await bareMember.PutAsync($"/v1/guilds/{id}/members/{10_006}/roles/{roleId}", content: null);

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownRole_Returns_404()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_007);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;
		await factory.AddBareMemberAsync(long.Parse(id), userId: 10_008);

		var resp = await owner.PutAsync($"/v1/guilds/{id}/members/{10_008}/roles/9999999", content: null);
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task TargetNotAMember_Returns_404()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_009);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		var roleId = await factory.SeedRoleAsync(id, "Mod", position: 5, permissions: 0L);

		var resp = await owner.PutAsync($"/v1/guilds/{id}/members/{10_010}/roles/{roleId}", content: null);
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task AssigningDefaultRole_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_011);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		var everyoneId = await factory.GetEveryoneRoleIdAsync(id);
		await factory.AddBareMemberAsync(id, userId: 10_012);

		var resp = await owner.PutAsync($"/v1/guilds/{id}/members/{10_012}/roles/{everyoneId}", content: null);
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task RoleAboveCallerInHierarchy_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_013);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);

		// manager seeded at position 99 with ManageRoles
		await factory.AddMemberWithPermissionsAsync(id, userId: 10_014,
			permissions: (long)Permission.ManageRoles);
		await factory.AddBareMemberAsync(id, userId: 10_015);

		// role at position 200, above the manager
		var highRoleId = await factory.SeedRoleAsync(id, "TopBrass", position: 200, permissions: 0L);

		var manager = factory.CreateAuthenticatedClient(userId: 10_014);
		var resp = await manager.PutAsync($"/v1/guilds/{id}/members/{10_015}/roles/{highRoleId}", content: null);
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task RoleHasPermissionsCallerLacks_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_016);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);

		// manager: position 99, has ManageRoles only
		await factory.AddMemberWithPermissionsAsync(id, userId: 10_017,
			permissions: (long)Permission.ManageRoles);
		await factory.AddBareMemberAsync(id, userId: 10_018);

		// role: position 50 (below manager so hierarchy passes), grants BanMembers
		var banRoleId = await factory.SeedRoleAsync(id, "Banhammer", position: 50,
			permissions: (long)Permission.BanMembers);

		var manager = factory.CreateAuthenticatedClient(userId: 10_017);
		var resp = await manager.PutAsync($"/v1/guilds/{id}/members/{10_018}/roles/{banRoleId}", content: null);
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_204_AndShowsUpInPermissions()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_019);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;
		await factory.AddBareMemberAsync(long.Parse(id), userId: 10_020);

		var created = await owner.PostAsJsonAsync($"/v1/guilds/{id}/roles", new
		{
			name = "Mod",
			permissions = (long)Permission.KickMembers,
		});
		var roleId = (await created.ReadJsonAsync()).GetProperty("id").GetString();

		var assign = await owner.PutAsync($"/v1/guilds/{id}/members/{10_020}/roles/{roleId}", content: null);
		Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);

		var perms = await owner.GetAsync($"/v1/guilds/{id}/members/{10_020}/permissions");
		Assert.Equal(HttpStatusCode.OK, perms.StatusCode);
		var body = await perms.ReadJsonAsync();
		var roleIds = body.GetProperty("roles").EnumerateArray()
			.Select(r => r.GetProperty("id").GetString())
			.ToList();
		Assert.Contains(roleId, roleIds);
		var expected = ((long)Permission.SendMessages | (long)Permission.ReadMessages
			| (long)Permission.CreateInvite | (long)Permission.KickMembers);
		Assert.Equal(expected, body.GetProperty("effective_permissions").GetInt64());
	}

	[Fact]
	public async Task Idempotent_DuplicateAssign_Returns_204()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_021);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;
		await factory.AddBareMemberAsync(long.Parse(id), userId: 10_022);

		var created = await owner.PostAsJsonAsync($"/v1/guilds/{id}/roles", new { name = "Mod" });
		var roleId = (await created.ReadJsonAsync()).GetProperty("id").GetString();

		var first = await owner.PutAsync($"/v1/guilds/{id}/members/{10_022}/roles/{roleId}", content: null);
		var second = await owner.PutAsync($"/v1/guilds/{id}/members/{10_022}/roles/{roleId}", content: null);

		Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
	}
}
