using System.Net;
using System.Net.Http.Json;
using Guild.Domain.Guild;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class UpdateNicknameTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.PatchAsJsonAsync("/v1/guilds/1/members/1", new { nickname = "x" });
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 9201);
		var resp = await client.PatchAsJsonAsync("/v1/guilds/9999999/members/9201", new { nickname = "x" });
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task TargetNotAMember_Returns_404()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9202);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.PatchAsJsonAsync($"/v1/guilds/{id}/members/999999", new { nickname = "x" });

		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task Self_Returns_200_AndUpdatesNickname()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9203);
		var guild = await owner.CreateGuildAsync("g");
		var gid = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(gid, userId: 9204);

		var member = factory.CreateAuthenticatedClient(userId: 9204);
		var resp = await member.PatchAsJsonAsync($"/v1/guilds/{gid}/members/9204", new { nickname = "Spuffie" });

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal("9204", body.GetProperty("user_id").GetString());
		Assert.Equal("Spuffie", body.GetProperty("nickname").GetString());
	}

	[Fact]
	public async Task Self_NullClears_Nickname()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9205);
		var guild = await owner.CreateGuildAsync("g");
		var gid = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(gid, userId: 9206);

		var member = factory.CreateAuthenticatedClient(userId: 9206);
		var set = await member.PatchAsJsonAsync($"/v1/guilds/{gid}/members/9206", new { nickname = "tmp" });
		Assert.Equal(HttpStatusCode.OK, set.StatusCode);

		var clear = await member.PatchAsJsonAsync($"/v1/guilds/{gid}/members/9206", new { nickname = (string?)null });
		Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
		var body = await clear.ReadJsonAsync();
		Assert.Equal(System.Text.Json.JsonValueKind.Null, body.GetProperty("nickname").ValueKind);
	}

	[Fact]
	public async Task Other_WithoutManageNicknames_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9207);
		var guild = await owner.CreateGuildAsync("g");
		var gid = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(gid, userId: 9208);
		await factory.AddBareMemberAsync(gid, userId: 9209);

		var member = factory.CreateAuthenticatedClient(userId: 9208);
		var resp = await member.PatchAsJsonAsync($"/v1/guilds/{gid}/members/9209", new { nickname = "rude" });

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task Other_WithManageNicknames_OutRanksTarget_Returns_200()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9210);
		var guild = await owner.CreateGuildAsync("g");
		var gid = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddMemberWithPermissionsAsync(gid, userId: 9211, permissions: (long)Permission.ManageNicknames);
		await factory.AddBareMemberAsync(gid, userId: 9212);

		var mod = factory.CreateAuthenticatedClient(userId: 9211);
		var resp = await mod.PatchAsJsonAsync($"/v1/guilds/{gid}/members/9212", new { nickname = "renamed" });

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.Equal("renamed", body.GetProperty("nickname").GetString());
	}

	[Fact]
	public async Task NicknameTooLong_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9213);
		var guild = await owner.CreateGuildAsync("g");
		var gid = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(gid, userId: 9214);

		var member = factory.CreateAuthenticatedClient(userId: 9214);
		var resp = await member.PatchAsJsonAsync(
			$"/v1/guilds/{gid}/members/9214",
			new { nickname = new string('a', 65) });

		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task NicknameWithControlChar_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9215);
		var guild = await owner.CreateGuildAsync("g");
		var gid = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(gid, userId: 9216);

		var member = factory.CreateAuthenticatedClient(userId: 9216);
		var resp = await member.PatchAsJsonAsync(
			$"/v1/guilds/{gid}/members/9216",
			new { nickname = "bad\nname" });

		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}
}
