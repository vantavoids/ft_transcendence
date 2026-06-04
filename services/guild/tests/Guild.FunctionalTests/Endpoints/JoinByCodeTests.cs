using System.Net;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class JoinByCodeTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.PostAsync("/v1/invites/code/join", content: null);
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownCode_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 7701);
		var resp = await client.PostAsync("/v1/invites/no-such-code/join", content: null);
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_200_WithGuildBody()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7702);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddInviteAsync(id, "join-by-code", createdBy: 7702);

		var stranger = factory.CreateAuthenticatedClient(userId: 7703);
		var resp = await stranger.PostAsync("/v1/invites/join-by-code/join", content: null);

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(id.ToString(), body.GetProperty("id").GetString());
	}

	[Fact]
	public async Task AlreadyMember_Returns_409()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7704);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddInviteAsync(id, "already-in", createdBy: 7704);

		var resp = await owner.PostAsync("/v1/invites/already-in/join", content: null);
		Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
	}
}
