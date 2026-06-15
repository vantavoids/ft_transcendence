using System.Net;
using System.Net.Http.Json;
using Guild.Domain.Guild;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class UnassignRoleTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.DeleteAsync("/v1/guilds/1/members/2/roles/3");
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 10_101);
		var resp = await client.DeleteAsync("/v1/guilds/9999999/members/2/roles/3");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task CallerNotAMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_102);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 10_103);
		var resp = await stranger.DeleteAsync($"/v1/guilds/{id}/members/{10_102}/roles/1");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task WithoutManageRolesPerm_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_104);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(id, userId: 10_105);
		await factory.AddBareMemberAsync(id, userId: 10_106);
		var roleId = await factory.SeedRoleAsync(id, "Mod", position: 50, permissions: 0L);

		var bareMember = factory.CreateAuthenticatedClient(userId: 10_105);
		var resp = await bareMember.DeleteAsync($"/v1/guilds/{id}/members/{10_106}/roles/{roleId}");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownRole_Returns_404()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_107);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;
		await factory.AddBareMemberAsync(long.Parse(id), userId: 10_108);

		var resp = await owner.DeleteAsync($"/v1/guilds/{id}/members/{10_108}/roles/9999999");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task TargetNotAMember_Returns_404()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_109);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		var roleId = await factory.SeedRoleAsync(id, "Mod", position: 5, permissions: 0L);

		var resp = await owner.DeleteAsync($"/v1/guilds/{id}/members/{10_110}/roles/{roleId}");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task UnassigningDefaultRole_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_111);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		var everyoneId = await factory.GetEveryoneRoleIdAsync(id);
		await factory.AddBareMemberAsync(id, userId: 10_112);

		var resp = await owner.DeleteAsync($"/v1/guilds/{id}/members/{10_112}/roles/{everyoneId}");
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task RoleAboveCallerInHierarchy_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_113);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);

		await factory.AddMemberWithPermissionsAsync(id, userId: 10_114,
			permissions: (long)Permission.ManageRoles);
		await factory.AddBareMemberAsync(id, userId: 10_115);
		var highRoleId = await factory.SeedRoleAsync(id, "TopBrass", position: 200, permissions: 0L);

		var manager = factory.CreateAuthenticatedClient(userId: 10_114);
		var resp = await manager.DeleteAsync($"/v1/guilds/{id}/members/{10_115}/roles/{highRoleId}");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task AssignmentNotPresent_Returns_404()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_116);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(id, userId: 10_117);
		var roleId = await factory.SeedRoleAsync(id, "Mod", position: 5, permissions: 0L);

		var resp = await owner.DeleteAsync($"/v1/guilds/{id}/members/{10_117}/roles/{roleId}");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_204_AndRemovesRole()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_118);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;
		await factory.AddBareMemberAsync(long.Parse(id), userId: 10_119);

		var created = await owner.PostAsJsonAsync($"/v1/guilds/{id}/roles", new
		{
			name = "Mod",
			permissions = (long)Permission.KickMembers,
		});
		var roleId = (await created.ReadJsonAsync()).GetProperty("id").GetString();

		var assign = await owner.PutAsync($"/v1/guilds/{id}/members/{10_119}/roles/{roleId}", content: null);
		Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);

		var unassign = await owner.DeleteAsync($"/v1/guilds/{id}/members/{10_119}/roles/{roleId}");
		Assert.Equal(HttpStatusCode.NoContent, unassign.StatusCode);

		var perms = await owner.GetAsync($"/v1/guilds/{id}/members/{10_119}/permissions");
		var body = await perms.ReadJsonAsync();
		var roleIds = body.GetProperty("roles").EnumerateArray()
			.Select(r => r.GetProperty("id").GetString())
			.ToList();
		Assert.DoesNotContain(roleId, roleIds);
	}
}
