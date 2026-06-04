using System.Net;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class GetInvitePreviewTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.GetAsync("/v1/invites/some-code");
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownCode_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 7601);
		var resp = await client.GetAsync("/v1/invites/no-such-code");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_ReturnsPreviewWithGuildAndInviter()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7602);
		var guild = await owner.CreateGuildAsync("My Guild");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddInviteAsync(id, "preview-me", createdBy: 7602);

		var client = factory.CreateAuthenticatedClient(userId: 7603);
		var resp = await client.GetAsync("/v1/invites/preview-me");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

		var body = await resp.ReadJsonAsync();
		Assert.Equal("preview-me", body.GetProperty("code").GetString());

		var guildObj = body.GetProperty("guild");
		Assert.Equal(id.ToString(), guildObj.GetProperty("id").GetString());
		Assert.Equal("My Guild", guildObj.GetProperty("name").GetString());
		Assert.Equal(1, guildObj.GetProperty("member_count").GetInt32());

		var inviter = body.GetProperty("inviter");
		Assert.Equal("7602", inviter.GetProperty("id").GetString());
		// NoopUserService returns deterministic "user{id}" summaries
		Assert.Equal("user7602", inviter.GetProperty("username").GetString());
	}
}
