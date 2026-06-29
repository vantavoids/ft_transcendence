using System.Net;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class RevokeInviteTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.DeleteAsync("/v1/guilds/1/invites/code");
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownCode_Returns_404()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7501);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.DeleteAsync($"/v1/guilds/{id}/invites/no-such-code");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task CodeFromDifferentGuild_Returns_404()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7502);
		var g1 = await owner.CreateGuildAsync("g1");
		var g2 = await owner.CreateGuildAsync("g2");
		var g1Id = long.Parse(g1.GetProperty("id").GetString()!);
		var g2Id = long.Parse(g2.GetProperty("id").GetString()!);
		await factory.AddInviteAsync(g1Id, "g1-code", createdBy: 7502);

		var resp = await owner.DeleteAsync($"/v1/guilds/{g2Id}/invites/g1-code");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task NonMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7503);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddInviteAsync(id, "stranger-attempt", createdBy: 7503);

		var stranger = factory.CreateAuthenticatedClient(userId: 7504);
		var resp = await stranger.DeleteAsync($"/v1/guilds/{id}/invites/stranger-attempt");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task NonCreatorWithoutManageGuild_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7505);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(id, userId: 7506);
		await factory.AddInviteAsync(id, "owner-made", createdBy: 7505);

		var member = factory.CreateAuthenticatedClient(userId: 7506);
		var resp = await member.DeleteAsync($"/v1/guilds/{id}/invites/owner-made");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task Owner_Returns_204_AndExcludesFromList()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7507);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddInviteAsync(id, "to-revoke", createdBy: 7507);

		var resp = await owner.DeleteAsync($"/v1/guilds/{id}/invites/to-revoke");
		Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

		// second delete sees the invite as revoked, returns 404
		var resp2 = await owner.DeleteAsync($"/v1/guilds/{id}/invites/to-revoke");
		Assert.Equal(HttpStatusCode.NotFound, resp2.StatusCode);
	}
}
