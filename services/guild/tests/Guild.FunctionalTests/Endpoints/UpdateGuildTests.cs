using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class UpdateGuildTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.PatchAsJsonAsync(
			"/v1/guilds/1",
			new { name = "x" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task Non_Member_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 2001);
		var stranger = factory.CreateAuthenticatedClient(userId: 2002);

		var created = await owner.CreateGuildAsync("locked");
		var id = created.GetProperty("id").GetString()!;

		var resp = await stranger.PatchAsJsonAsync(
			$"/v1/guilds/{id}",
			new { name = "hacked" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task Owner_Updates_Name_Returns_200()
	{
		var client = factory.CreateAuthenticatedClient(userId: 2003);
		var created = await client.CreateGuildAsync("before");
		var id = created.GetProperty("id").GetString()!;

		var resp = await client.PatchAsJsonAsync(
			$"/v1/guilds/{id}",
			new { name = "after" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

		var root = await resp.ReadJsonAsync();
		Assert.Equal("after", root.GetProperty("name").GetString());
		Assert.Equal(id, root.GetProperty("id").GetString());
	}

	[Fact]
	public async Task Partial_Update_Leaves_Other_Fields_Unchanged()
	{
		var client = factory.CreateAuthenticatedClient(userId: 2004);
		var created = await client.CreateGuildAsync("partial", description: "keep me");
		var id = created.GetProperty("id").GetString()!;

		// PATCH only name; description must stay
		var resp = await client.PatchAsJsonAsync(
			$"/v1/guilds/{id}",
			new { name = "renamed" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var root = await resp.ReadJsonAsync();
		Assert.Equal("renamed", root.GetProperty("name").GetString());
		Assert.Equal("keep me", root.GetProperty("description").GetString());
	}
}
