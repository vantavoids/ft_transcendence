using System.Net;
using Guild.Domain.Guild;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class GetVisibleChannelsTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task GuildLessUser_Returns200EmptyArray()
	{
		var anon = factory.CreateClient();
		var resp = await anon.GetAsync("/internal/users/777777/channels");

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(0, body.GetArrayLength());
	}

	[Fact]
	public async Task NotReachableUnder_V1_PathPrefix()
	{
		var anon = factory.CreateClient();
		var resp = await anon.GetAsync("/v1/users/1/channels");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task Owner_SeesGuildChannels()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 6001);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "general");

		var anon = factory.CreateClient();
		var resp = await anon.GetAsync("/internal/users/6001/channels");

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(1, body.GetArrayLength());
		var item = body[0];
		Assert.Equal(channelId.ToString(), item.GetProperty("id").GetString());
		Assert.Equal(guildId.ToString(), item.GetProperty("guild_id").GetString());
	}

	[Fact]
	public async Task MemberWithReadDenied_ChannelIsExcluded()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 6002);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var visibleChannelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "general");
		var hiddenChannelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "secret");
		await factory.AddBareMemberAsync(guildId, userId: 6003);

		await factory.AddChannelOverwriteAsync(
			hiddenChannelId, OverwriteTargetType.Member, targetId: 6003,
			allow: 0L, deny: (long)Permission.ReadMessages);

		var anon = factory.CreateClient();
		var resp = await anon.GetAsync("/internal/users/6003/channels");

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(1, body.GetArrayLength());
		Assert.Equal(visibleChannelId.ToString(), body[0].GetProperty("id").GetString());
	}

	[Fact]
	public async Task InvalidUserId_Returns400()
	{
		var anon = factory.CreateClient();
		var resp = await anon.GetAsync("/internal/users/-1/channels");
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}
}
