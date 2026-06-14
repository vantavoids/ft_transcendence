using System.Net;
using System.Net.Http.Json;
using Guild.Domain.Guild;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class DeleteRoleTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.DeleteAsync("/v1/guilds/1/roles/1");
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 9001);
		var resp = await client.DeleteAsync("/v1/guilds/9999999/roles/1");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownRole_Returns_404()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9002);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.DeleteAsync($"/v1/guilds/{id}/roles/9999999");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task DefaultRole_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9003);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;
		var everyoneId = await factory.GetEveryoneRoleIdAsync(long.Parse(id));

		var resp = await owner.DeleteAsync($"/v1/guilds/{id}/roles/{everyoneId}");
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task WithoutManageRolesPerm_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9004);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		// owner creates a deletable role
		var created = await owner.PostAsJsonAsync($"/v1/guilds/{id}/roles", new { name = "Mod" });
		var roleId = (await created.ReadJsonAsync()).GetProperty("id").GetString();

		// stranger member without MANAGE_ROLES tries to delete it
		await factory.AddBareMemberAsync(long.Parse(id), userId: 9005);
		var member = factory.CreateAuthenticatedClient(userId: 9005);
		var resp = await member.DeleteAsync($"/v1/guilds/{id}/roles/{roleId}");

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_204_AndRoleIsGone()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9006);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var created = await owner.PostAsJsonAsync($"/v1/guilds/{id}/roles", new { name = "Mod" });
		var roleId = (await created.ReadJsonAsync()).GetProperty("id").GetString();

		var deleted = await owner.DeleteAsync($"/v1/guilds/{id}/roles/{roleId}");
		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

		// second delete should now 404
		var second = await owner.DeleteAsync($"/v1/guilds/{id}/roles/{roleId}");
		Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
	}
}
