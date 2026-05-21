using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Guild.Domain.Guild;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

// GET /channels/{channel_id}/permissions
// contract: member with MANAGE_CHANNELS gets 200 + list; non-member gets 403;
// missing permission gets 403; unknown channel gets 404
public sealed class GetChannelOverwriteTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Unknown_Channel_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5601);
		var resp = await client.GetAsync("/v1/channels/999999/permissions");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task NonMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5602);
		var stranger = factory.CreateAuthenticatedClient(userId: 5603);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var resp = await stranger.GetAsync($"/v1/channels/{channelId}/permissions");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task MemberWithoutManageChannels_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5604);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");
		await factory.AddBareMemberAsync(guildId, userId: 5605);

		var member = factory.CreateAuthenticatedClient(userId: 5605);
		var resp = await member.GetAsync($"/v1/channels/{channelId}/permissions");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task MemberWithManageChannels_Returns_200()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5606);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var resp = await owner.GetAsync($"/v1/channels/{channelId}/permissions");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
	}

	[Fact]
	public async Task NoOverwrites_ReturnsEmptyList()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5607);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var resp = await owner.GetAsync($"/v1/channels/{channelId}/permissions");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal(JsonValueKind.Array, body.ValueKind);
		Assert.Equal(0, body.GetArrayLength());
	}

	[Fact]
	public async Task RoleOverwrite_ShapeIsCorrect()
	{
		// contract shape: { target_id, target_type, allow, deny }
		var owner = factory.CreateAuthenticatedClient(userId: 5608);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");
		var roleId = await factory.GetEveryoneRoleIdAsync(guildId);

		await factory.AddChannelOverwriteAsync(channelId, OverwriteTargetType.Role, roleId, allow: 4L, deny: 1L);

		var resp = await owner.GetAsync($"/v1/channels/{channelId}/permissions");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var list = await resp.ReadJsonAsync();
		Assert.Equal(1, list.GetArrayLength());

		var ow = list[0];
		Assert.Equal(roleId.ToString(), ow.GetProperty("target_id").GetString());
		Assert.Equal("role", ow.GetProperty("target_type").GetString());
		Assert.Equal(4L, ow.GetProperty("allow").GetInt64());
		Assert.Equal(1L, ow.GetProperty("deny").GetInt64());
	}

	[Fact]
	public async Task MemberOverwrite_TargetTypeIsUser()
	{
		// member-targeted overwrites must serialise as "user", not "member"
		var owner = factory.CreateAuthenticatedClient(userId: 5609);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		await factory.AddChannelOverwriteAsync(channelId, OverwriteTargetType.Member, targetId: 5609, allow: 2L, deny: 0L);

		var resp = await owner.GetAsync($"/v1/channels/{channelId}/permissions");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var list = await resp.ReadJsonAsync();
		Assert.Equal(1, list.GetArrayLength());
		Assert.Equal("user", list[0].GetProperty("target_type").GetString());
		Assert.Equal("5609", list[0].GetProperty("target_id").GetString());
	}

	[Fact]
	public async Task MultipleOverwrites_ReturnsAll()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5610);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");
		var roleId = await factory.GetEveryoneRoleIdAsync(guildId);

		await factory.AddChannelOverwriteAsync(channelId, OverwriteTargetType.Role, roleId, allow: 1L, deny: 0L);
		await factory.AddChannelOverwriteAsync(channelId, OverwriteTargetType.Member, targetId: 5610, allow: 2L, deny: 0L);

		var resp = await owner.GetAsync($"/v1/channels/{channelId}/permissions");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var list = await resp.ReadJsonAsync();
		Assert.Equal(2, list.GetArrayLength());
	}
}
