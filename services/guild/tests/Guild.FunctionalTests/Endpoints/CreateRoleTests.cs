using System.Net;
using System.Net.Http.Json;
using Guild.Domain.Guild;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class CreateRoleTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.PostAsJsonAsync("/v1/guilds/1/roles", new { name = "Mod" });
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 9801);
		var resp = await client.PostAsJsonAsync("/v1/guilds/9999999/roles", new { name = "Mod" });
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task NotAMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9802);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 9803);
		var resp = await stranger.PostAsJsonAsync($"/v1/guilds/{id}/roles", new { name = "Mod" });

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task WithoutManageRolesPerm_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9804);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(id, userId: 9805);

		var member = factory.CreateAuthenticatedClient(userId: 9805);
		var resp = await member.PostAsJsonAsync($"/v1/guilds/{id}/roles", new { name = "Mod" });

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task AttemptingToGrantPermissionsCallerLacks_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9806);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddMemberWithPermissionsAsync(id, userId: 9807,
			permissions: (long)Permission.ManageRoles);

		var manager = factory.CreateAuthenticatedClient(userId: 9807);
		var resp = await manager.PostAsJsonAsync($"/v1/guilds/{id}/roles", new
		{
			name = "Banhammer",
			permissions = (long)Permission.BanMembers,
		});

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task InvalidName_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9808);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.PostAsJsonAsync($"/v1/guilds/{id}/roles", new { name = "" });
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task InvalidColor_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9809);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.PostAsJsonAsync($"/v1/guilds/{id}/roles",
			new { name = "Mod", color = "not-a-hex" });
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_201_WithRoleObject()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9810);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.PostAsJsonAsync($"/v1/guilds/{id}/roles", new
		{
			name = "Moderator",
			color = "#FF5733",
			permissions = (long)Permission.KickMembers,
			is_hoisted = true,
		});

		Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal("Moderator", body.GetProperty("name").GetString());
		Assert.Equal("#FF5733", body.GetProperty("color").GetString());
		Assert.Equal((long)Permission.KickMembers, body.GetProperty("permissions").GetInt64());
		Assert.True(body.GetProperty("is_hoisted").GetBoolean());
		Assert.False(body.GetProperty("is_default").GetBoolean());
		// pre-existing roles are @everyone(0) + Administrator(1), so new role at 2
		Assert.Equal(2, body.GetProperty("position").GetInt32());
	}
}
