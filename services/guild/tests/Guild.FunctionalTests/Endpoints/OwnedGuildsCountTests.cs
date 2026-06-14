using System.Net;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class OwnedGuildsCountTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task NoToken_StillOk_BecauseEndpointIsOnInternalGroup()
	{
		// the Auth Service mimics this with internal docker traffic;
		// /internal/... bypasses RequireAuthorization on the /v1 group
		var anon = factory.CreateClient();
		var resp = await anon.GetAsync("/internal/users/1/owned-guilds-count");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
	}

	[Fact]
	public async Task NotReachableUnder_V1_PathPrefix()
	{
		// gateway only forwards /api/{service}/vN/...; the upstream must NOT mount
		// this under /v1 (it would otherwise be reachable from outside the docker
		// network via /api/guild/v1/users/.../owned-guilds-count)
		var anon = factory.CreateClient();
		var resp = await anon.GetAsync("/v1/users/1/owned-guilds-count");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownUser_Returns_OkWithZeroCount()
	{
		var anon = factory.CreateClient();
		var resp = await anon.GetAsync("/internal/users/999999/owned-guilds-count");

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(0, body.GetProperty("count").GetInt32());
	}

	[Fact]
	public async Task OwnerOfOneGuild_Returns_One()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9601);
		await owner.CreateGuildAsync("g");

		var anon = factory.CreateClient();
		var resp = await anon.GetAsync("/internal/users/9601/owned-guilds-count");

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(1, body.GetProperty("count").GetInt32());
	}

	[Fact]
	public async Task OwnerOfMultipleGuilds_Returns_AccurateCount()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9602);
		await owner.CreateGuildAsync("a");
		await owner.CreateGuildAsync("b");
		await owner.CreateGuildAsync("c");

		var anon = factory.CreateClient();
		var resp = await anon.GetAsync("/internal/users/9602/owned-guilds-count");

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(3, body.GetProperty("count").GetInt32());
	}

	[Fact]
	public async Task UserWhoIsAMemberButNotOwner_Returns_Zero()
	{
		// the contract is specifically owned-guilds, not membership. a member of
		// someone else's guild should not show up here
		var owner = factory.CreateAuthenticatedClient(userId: 9603);
		var guild = await owner.CreateGuildAsync("g");
		var gid = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(gid, userId: 9604);

		var anon = factory.CreateClient();
		var resp = await anon.GetAsync("/internal/users/9604/owned-guilds-count");

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(0, body.GetProperty("count").GetInt32());
	}

	[Fact]
	public async Task NonPositiveUserId_Returns_400()
	{
		var anon = factory.CreateClient();
		// negative is rejected by route constraint (long:long parser allows it, but
		// our handler validates > 0). zero also gets rejected.
		var resp = await anon.GetAsync("/internal/users/0/owned-guilds-count");
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}
}
