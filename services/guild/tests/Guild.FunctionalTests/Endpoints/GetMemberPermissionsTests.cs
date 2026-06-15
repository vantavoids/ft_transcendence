using System.Net;
using Guild.Domain.Guild;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class GetMemberPermissionsTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	private const long EveryonePerms =
		(long)Permission.SendMessages | (long)Permission.ReadMessages | (long)Permission.CreateInvite;

	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.GetAsync("/v1/guilds/1/members/2/permissions");
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 10_201);
		var resp = await client.GetAsync("/v1/guilds/9999999/members/2/permissions");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task CallerNotAMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_202);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 10_203);
		var resp = await stranger.GetAsync($"/v1/guilds/{id}/members/{10_202}/permissions");
		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task TargetNotAMember_Returns_404()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_204);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.GetAsync($"/v1/guilds/{id}/members/{10_205}/permissions");
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task Owner_Returns_AdministratorBit_AndIsOwnerTrue()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_206);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.GetAsync($"/v1/guilds/{id}/members/{10_206}/permissions");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.True(body.GetProperty("is_owner").GetBoolean());
		Assert.Equal((long)Permission.Administrator,
			body.GetProperty("effective_permissions").GetInt64());
		// owner sees both @everyone (implicit) and the seeded Administrator role
		var roleNames = body.GetProperty("roles").EnumerateArray()
			.Select(r => r.GetProperty("name").GetString())
			.ToList();
		Assert.Contains("@everyone", roleNames);
		Assert.Contains("Administrator", roleNames);
	}

	[Fact]
	public async Task BareMember_Returns_EveryonePermissionsOnly()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_207);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(id, userId: 10_208);

		var member = factory.CreateAuthenticatedClient(userId: 10_208);
		var resp = await member.GetAsync($"/v1/guilds/{id}/members/{10_208}/permissions");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.False(body.GetProperty("is_owner").GetBoolean());
		Assert.Equal(EveryonePerms, body.GetProperty("effective_permissions").GetInt64());
		var roleNames = body.GetProperty("roles").EnumerateArray()
			.Select(r => r.GetProperty("name").GetString())
			.ToList();
		Assert.Equal(["@everyone"], roleNames);
	}

	[Fact]
	public async Task MemberWithCustomRole_Returns_AggregatedPermissions()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 10_209);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddMemberWithPermissionsAsync(id, userId: 10_210,
			permissions: (long)Permission.KickMembers);

		var member = factory.CreateAuthenticatedClient(userId: 10_210);
		var resp = await member.GetAsync($"/v1/guilds/{id}/members/{10_210}/permissions");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.False(body.GetProperty("is_owner").GetBoolean());
		Assert.Equal(EveryonePerms | (long)Permission.KickMembers,
			body.GetProperty("effective_permissions").GetInt64());
	}
}
