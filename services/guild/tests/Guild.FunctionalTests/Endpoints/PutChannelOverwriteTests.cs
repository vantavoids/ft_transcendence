using System.Net;
using System.Net.Http.Json;
using Guild.Domain.Guild;
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

	// BUG: handler checks ManageRoles instead of ManageChannels -- should return 200, currently 403
	[Fact]
	public async Task MemberWithManageChannels_CanPutOverwrite()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5407);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");
		var roleId = await factory.GetEveryoneRoleIdAsync(guildId);

		await factory.AddMemberWithPermissionsAsync(guildId, userId: 5408, (long)Permission.ManageChannels);
		var member = factory.CreateAuthenticatedClient(userId: 5408);

		var resp = await member.PutAsJsonAsync(
			$"/v1/channels/{channelId}/permissions/{roleId}",
			new { target_type = "role", allow = (long)Permission.SendMessages, deny = 0L },
			JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
	}

	// BUG: handler checks ManageRoles instead of ManageChannels -- ManageRoles should NOT suffice
	[Fact]
	public async Task MemberWithOnlyManageRoles_CannotPutOverwrite()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5409);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");
		var roleId = await factory.GetEveryoneRoleIdAsync(guildId);

		await factory.AddMemberWithPermissionsAsync(guildId, userId: 5410, (long)Permission.ManageRoles);
		var member = factory.CreateAuthenticatedClient(userId: 5410);

		var resp = await member.PutAsJsonAsync(
			$"/v1/channels/{channelId}/permissions/{roleId}",
			new { target_type = "role", allow = (long)Permission.SendMessages, deny = 0L },
			JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	// BUG: "user" keyword not accepted as alias for member target -- should return 200, currently 400
	[Fact]
	public async Task UserKeyword_AcceptedAsMemberTarget()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5411);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var resp = await owner.PutAsJsonAsync(
			$"/v1/channels/{channelId}/permissions/5411",
			new { target_type = "user", allow = (long)Permission.SendMessages, deny = 0L },
			JsonOptions.SnakeCase);
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
	}

	// BUG: response serialises target_type as "member" instead of "user"
	[Fact]
	public async Task MemberTargetOverwrite_ResponseTargetTypeIsUser()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5412);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var resp = await owner.PutAsJsonAsync(
			$"/v1/channels/{channelId}/permissions/5412",
			new { target_type = "member", allow = (long)Permission.SendMessages, deny = 0L },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal("user", body.GetProperty("target_type").GetString());
	}
}
