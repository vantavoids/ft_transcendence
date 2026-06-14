using System.Net;
using System.Net.Http.Json;
using Guild.Domain.Guild;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class UpdateRoleTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.PatchAsJsonAsync("/v1/guilds/1/roles/1", new { name = "X" });
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 9901);
		var resp = await client.PatchAsJsonAsync("/v1/guilds/9999999/roles/1", new { name = "X" });
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownRole_Returns_404()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9902);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.PatchAsJsonAsync($"/v1/guilds/{id}/roles/9999999", new { name = "X" });
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task DefaultRole_NameChange_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9903);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;
		var everyoneId = await factory.GetEveryoneRoleIdAsync(long.Parse(id));

		var resp = await owner.PatchAsJsonAsync(
			$"/v1/guilds/{id}/roles/{everyoneId}", new { name = "renamed" });
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task DefaultRole_PermissionsChange_Returns_200()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9904);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;
		var everyoneId = await factory.GetEveryoneRoleIdAsync(long.Parse(id));

		var resp = await owner.PatchAsJsonAsync(
			$"/v1/guilds/{id}/roles/{everyoneId}",
			new { permissions = (long)Permission.SendMessages });

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal((long)Permission.SendMessages, body.GetProperty("permissions").GetInt64());
	}

	[Fact]
	public async Task WithoutManageRolesPerm_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9905);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		var everyoneId = await factory.GetEveryoneRoleIdAsync(id);
		await factory.AddBareMemberAsync(id, userId: 9906);

		var member = factory.CreateAuthenticatedClient(userId: 9906);
		var resp = await member.PatchAsJsonAsync(
			$"/v1/guilds/{id}/roles/{everyoneId}",
			new { permissions = (long)Permission.SendMessages });

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_200_AndUpdatesRole()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9907);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var created = await owner.PostAsJsonAsync($"/v1/guilds/{id}/roles", new { name = "Mod" });
		Assert.Equal(HttpStatusCode.Created, created.StatusCode);
		var roleId = (await created.ReadJsonAsync()).GetProperty("id").GetString();

		var resp = await owner.PatchAsJsonAsync(
			$"/v1/guilds/{id}/roles/{roleId}",
			new { name = "Senior Mod", color = "#123456", is_hoisted = true });

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal("Senior Mod", body.GetProperty("name").GetString());
		Assert.Equal("#123456", body.GetProperty("color").GetString());
		Assert.True(body.GetProperty("is_hoisted").GetBoolean());
	}
}
