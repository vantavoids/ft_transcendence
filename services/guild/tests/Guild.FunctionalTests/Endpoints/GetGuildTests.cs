using System.Net;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class GetGuildTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.GetAsync("/v1/guilds/1");

		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task Unknown_Id_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 1001);
		var resp = await client.GetAsync("/v1/guilds/9999999999999");

		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task Caller_Not_Member_Returns_403_WithErrorBody()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 1002);
		var stranger = factory.CreateAuthenticatedClient(userId: 1003);

		var created = await owner.CreateGuildAsync("le transcendage");
		var id = created.GetProperty("id").GetString()!;

		var resp = await stranger.GetAsync($"/v1/guilds/{id}");

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.True(body.TryGetProperty("error", out var err));
		Assert.False(string.IsNullOrEmpty(err.GetString()));
	}

	[Fact]
	public async Task Owner_Returns_200_WithMemberCount()
	{
		var client = factory.CreateAuthenticatedClient(userId: 1004);
		var created = await client.CreateGuildAsync("bess-f2", description: "my domain. my home. my EVERYTHING <3");
		var id = created.GetProperty("id").GetString()!;

		var resp = await client.GetAsync($"/v1/guilds/{id}");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

		var root = await resp.ReadJsonAsync();
		Assert.Equal(id, root.GetProperty("id").GetString());
		Assert.Equal("bess-f2", root.GetProperty("name").GetString());
		Assert.Equal("my domain. my home. my EVERYTHING <3", root.GetProperty("description").GetString());
		Assert.Equal("1004", root.GetProperty("owner_id").GetString());
		Assert.Equal(1, root.GetProperty("member_count").GetInt32());
	}

	[Fact]
	public async Task Snowflake_Id_Roundtrips_As_String()
	{
		// regression guard for the JS precision-loss issue: confirm the id returned
		// by POST can be passed verbatim back into GET and resolve to the same row
		var client = factory.CreateAuthenticatedClient(userId: 1005);
		var created = await client.CreateGuildAsync("ça s'en va et ça revient askip");
		var id = created.GetProperty("id").GetString()!;

		// IDs that fit a long but exceed Number.MAX_SAFE_INTEGER must survive
		// as strings end-to-end. the handler's :long route still binds them
		var resp = await client.GetAsync($"/v1/guilds/{id}");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

		var root = await resp.ReadJsonAsync();
		Assert.Equal(id, root.GetProperty("id").GetString());
	}
}
