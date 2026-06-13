using System.Net;
using Guild.Domain.Guild;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class KickMemberTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.DeleteAsync("/v1/guilds/1/members/2");
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 9301);
		var resp = await client.DeleteAsync("/v1/guilds/9999999/members/2");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task TargetNotAMember_Returns_404()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9302);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.DeleteAsync($"/v1/guilds/{id}/members/999999");

		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task CallerNotMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9303);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 9304);
		var resp = await stranger.DeleteAsync($"/v1/guilds/{id}/members/9303");

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task WithoutKickMembersPerm_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9305);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(id, userId: 9306);
		await factory.AddBareMemberAsync(id, userId: 9307);

		var member = factory.CreateAuthenticatedClient(userId: 9306);
		var resp = await member.DeleteAsync($"/v1/guilds/{id}/members/9307");

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task OwnerAsTarget_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9308);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddMemberWithPermissionsAsync(id, userId: 9309, permissions: (long)Permission.KickMembers);

		var mod = factory.CreateAuthenticatedClient(userId: 9309);
		var resp = await mod.DeleteAsync($"/v1/guilds/{id}/members/9308");

		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_204_AndMemberIsGone()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9310);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddMemberWithPermissionsAsync(id, userId: 9311, permissions: (long)Permission.KickMembers);
		await factory.AddBareMemberAsync(id, userId: 9312);

		var mod = factory.CreateAuthenticatedClient(userId: 9311);
		var kick = await mod.DeleteAsync($"/v1/guilds/{id}/members/9312");
		Assert.Equal(HttpStatusCode.NoContent, kick.StatusCode);

		// member is gone: a second kick should now 404
		var second = await mod.DeleteAsync($"/v1/guilds/{id}/members/9312");
		Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
	}
}
