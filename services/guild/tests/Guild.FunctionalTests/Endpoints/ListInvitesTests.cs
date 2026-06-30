using System.Net;
using Guild.FunctionalTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class ListInvitesTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.GetAsync("/v1/guilds/1/invites");
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task NonMember_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7401);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var stranger = factory.CreateAuthenticatedClient(userId: 7402);
		var resp = await stranger.GetAsync($"/v1/guilds/{id}/invites");

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task Member_Without_ManageGuild_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7403);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(id, userId: 7404);

		var member = factory.CreateAuthenticatedClient(userId: 7404);
		var resp = await member.GetAsync($"/v1/guilds/{id}/invites");

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task OwnerHappyPath_ExcludesRevoked()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 7405);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);

		await factory.AddInviteAsync(id, "active1", createdBy: 7405);
		await factory.AddInviteAsync(id, "active2", createdBy: 7405);
		await factory.AddInviteAsync(id, "revoked", createdBy: 7405);
		await RevokeAsync(id, "revoked");

		var resp = await owner.GetAsync($"/v1/guilds/{id}/invites");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

		var body = await resp.ReadJsonAsync();
		var codes = body.EnumerateArray().Select(e => e.GetProperty("code").GetString()).ToHashSet();
		Assert.Contains("active1", codes);
		Assert.Contains("active2", codes);
		Assert.DoesNotContain("revoked", codes);
	}

	private async Task RevokeAsync(long guildId, string code)
	{
		using var scope = factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<Persistence.Db.GuildDbContext>();
		var invite = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
			.FirstAsync(db.GuildInvites, i => i.Code == code);
		invite.Revoke();
		await db.SaveChangesAsync();
	}
}
