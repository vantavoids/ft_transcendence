using System.Net;
using System.Net.Http.Json;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class PutChannelOverwriteTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Unknown_Channel_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5401);
		var resp = await client.PutAsJsonAsync(
			"/v1/channels/999999/permissions/1",
			new { target_type = "role", allow = 1L, deny = 0L },
			JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task NonMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5402);
		var stranger = factory.CreateAuthenticatedClient(userId: 5403);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var resp = await stranger.PutAsJsonAsync(
			$"/v1/channels/{channelId}/permissions/1",
			new { target_type = "role", allow = 1L, deny = 0L },
			JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_RoleTarget_Creates()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5404);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var roleId = (await factory.GetEveryoneRoleIdAsync(guildId)).ToString();

		var resp = await owner.PutAsJsonAsync(
			$"/v1/channels/{channelId}/permissions/{roleId}",
			new { target_type = "role", allow = 4L, deny = 0L },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal("role", body.GetProperty("target_type").GetString());
		Assert.Equal(roleId, body.GetProperty("target_id").GetString());
		Assert.Equal(4L, body.GetProperty("allow").GetInt64());
	}

	[Fact]
	public async Task InvalidTargetType_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5405);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var resp = await owner.PutAsJsonAsync(
			$"/v1/channels/{channelId}/permissions/1",
			new { target_type = "weird", allow = 0L, deny = 0L },
			JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task AllowDenyOverlap_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5406);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var roleId = (await factory.GetEveryoneRoleIdAsync(guildId)).ToString();

		var resp = await owner.PutAsJsonAsync(
			$"/v1/channels/{channelId}/permissions/{roleId}",
			new { target_type = "role", allow = 5L, deny = 1L },
			JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}
}
