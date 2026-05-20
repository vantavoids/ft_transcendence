using System.Net;
using Guild.Domain.Guild;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class DeleteChannelOverwriteTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	// BUG: handler checks ManageRoles instead of ManageChannels -- should return 204, currently 403
	[Fact]
	public async Task MemberWithManageChannels_CanDeleteOverwrite()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5504);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");
		var roleId = await factory.GetEveryoneRoleIdAsync(guildId);
		await factory.AddChannelOverwriteAsync(channelId, OverwriteTargetType.Role, roleId, 1L, 0L);

		await factory.AddMemberWithPermissionsAsync(guildId, userId: 5505, (long)Permission.ManageChannels);
		var member = factory.CreateAuthenticatedClient(userId: 5505);

		var resp = await member.DeleteAsync($"/v1/channels/{channelId}/permissions/{roleId}");
		Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
	}

	// BUG: handler checks ManageRoles instead of ManageChannels -- ManageRoles should NOT suffice
	[Fact]
	public async Task MemberWithOnlyManageRoles_CannotDeleteOverwrite()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5506);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");
		var roleId = await factory.GetEveryoneRoleIdAsync(guildId);
		await factory.AddChannelOverwriteAsync(channelId, OverwriteTargetType.Role, roleId, 1L, 0L);

		await factory.AddMemberWithPermissionsAsync(guildId, userId: 5507, (long)Permission.ManageRoles);
		var member = factory.CreateAuthenticatedClient(userId: 5507);

		var resp = await member.DeleteAsync($"/v1/channels/{channelId}/permissions/{roleId}");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}


	[Fact]
	public async Task Unknown_Channel_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 5501);
		var resp = await client.DeleteAsync("/v1/channels/999999/permissions/1");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task NoMatchingOverwrite_Returns_404()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5502);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");

		var resp = await owner.DeleteAsync($"/v1/channels/{channelId}/permissions/12345");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_204()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 5503);
		var created = await owner.CreateGuildAsync("guild");
		var guildId = long.Parse(created.GetProperty("id").GetString()!);
		var channelId = await factory.AddChannelAsync(guildId, categoryId: null, name: "g");
		var roleId = await factory.GetEveryoneRoleIdAsync(guildId);
		await factory.AddChannelOverwriteAsync(channelId, OverwriteTargetType.Role, roleId, 1L, 0L);

		var resp = await owner.DeleteAsync($"/v1/channels/{channelId}/permissions/{roleId}");
		Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
	}
}
