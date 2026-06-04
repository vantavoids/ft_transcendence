using System.Net;
using System.Net.Http.Json;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class LeaveGuildTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.PostAsync("/v1/guilds/1/leave", content: null);
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 7201);
		var resp = await client.PostAsync("/v1/guilds/9999999/leave", content: null);
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task Owner_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7202);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.PostAsync($"/v1/guilds/{id}/leave", content: null);
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task NotAMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7203);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 7204);
		var resp = await stranger.PostAsync($"/v1/guilds/{id}/leave", content: null);

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_204()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7205);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(id, userId: 7206);

		var member = factory.CreateAuthenticatedClient(userId: 7206);
		var resp = await member.PostAsync($"/v1/guilds/{id}/leave", content: null);

		Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

		// confirm idempotence: a second leave fails with 403 (not a member)
		var resp2 = await member.PostAsync($"/v1/guilds/{id}/leave", content: null);
		Assert.Equal(HttpStatusCode.Forbidden, resp2.StatusCode);
	}
}
