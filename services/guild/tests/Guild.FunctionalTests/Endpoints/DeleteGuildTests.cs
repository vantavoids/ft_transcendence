using System.Net;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class DeleteGuildTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.DeleteAsync("/v1/guilds/1");

		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task Non_Owner_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 3001);
		var stranger = factory.CreateAuthenticatedClient(userId: 3002);

		var created = await owner.CreateGuildAsync("undeletable! muhahahaha");
		var id = created.GetProperty("id").GetString()!;

		var resp = await stranger.DeleteAsync($"/v1/guilds/{id}");

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task Owner_Deletes_Returns_204_AndSubsequent_Get_Is_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 3003);
		var created = await client.CreateGuildAsync("guild that definitely won't die lol");
		var id = created.GetProperty("id").GetString()!;

		var del = await client.DeleteAsync($"/v1/guilds/{id}");
		Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

		var get = await client.GetAsync($"/v1/guilds/{id}");
		Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
	}
}
