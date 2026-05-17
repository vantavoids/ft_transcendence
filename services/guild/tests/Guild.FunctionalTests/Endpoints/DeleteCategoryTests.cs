using System.Net;
using System.Net.Http.Json;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class DeleteCategoryTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.DeleteAsync("/v1/guilds/1/categories/1");

		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task Unknown_Guild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 6001);
		var resp = await client.DeleteAsync("/v1/guilds/999999/categories/1");

		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task Unknown_Category_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 6002);
		var created = await client.CreateGuildAsync("guild");
		var guildId = created.GetProperty("id").GetString()!;

		var resp = await client.DeleteAsync($"/v1/guilds/{guildId}/categories/999999");

		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task Non_Member_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 6003);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = created.GetProperty("id").GetString()!;

		var catResp = await owner.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/categories",
			new { name = "Cat" },
			JsonOptions.SnakeCase);
		var cat = await catResp.ReadJsonAsync();
		var catId = cat.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 6004);
		var resp = await stranger.DeleteAsync($"/v1/guilds/{guildId}/categories/{catId}");

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task Member_Without_Permission_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 6005);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);

		var catResp = await owner.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/categories",
			new { name = "Cat" },
			JsonOptions.SnakeCase);
		var cat = await catResp.ReadJsonAsync();
		var catId = cat.GetProperty("id").GetString()!;

		await factory.AddBareMemberAsync(guildId, userId: 6006);

		var member = factory.CreateAuthenticatedClient(userId: 6006);
		var resp = await member.DeleteAsync($"/v1/guilds/{guildId}/categories/{catId}");

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_204_AndSubsequent_Delete_Is_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 6007);
		var created = await client.CreateGuildAsync("guild");
		var guildId = created.GetProperty("id").GetString()!;

		var catResp = await client.PostAsJsonAsync(
			$"/v1/guilds/{guildId}/categories",
			new { name = "Cat" },
			JsonOptions.SnakeCase);
		var cat = await catResp.ReadJsonAsync();
		var catId = cat.GetProperty("id").GetString()!;

		var del = await client.DeleteAsync($"/v1/guilds/{guildId}/categories/{catId}");
		Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

		var second = await client.DeleteAsync($"/v1/guilds/{guildId}/categories/{catId}");
		Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
	}
}
