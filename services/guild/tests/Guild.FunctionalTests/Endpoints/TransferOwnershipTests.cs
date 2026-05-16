using System.Net;
using System.Net.Http.Json;
using Guild.Domain.Guild;
using Guild.Persistence.Db;
using Guild.FunctionalTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class TransferOwnershipTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.PatchAsJsonAsync(
			"/v1/guilds/1/owner",
			new { new_owner_id = "42" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task Non_Owner_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 4001);
		var stranger = factory.CreateAuthenticatedClient(userId: 4002);

		var created = await owner.CreateGuildAsync("ownership");
		var id = created.GetProperty("id").GetString()!;

		var resp = await stranger.PatchAsJsonAsync(
			$"/v1/guilds/{id}/owner",
			new { new_owner_id = "4002" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task Target_Not_Member_Returns_404_WithErrorBody()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 4003);
		var created = await owner.CreateGuildAsync("loneliness");
		var id = created.GetProperty("id").GetString()!;

		var resp = await owner.PatchAsJsonAsync(
			$"/v1/guilds/{id}/owner",
			new { new_owner_id = "9999" }, // user 9999 never joined
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
		var body = await resp.ReadJsonAsync();
		Assert.True(body.TryGetProperty("error", out var err));
		Assert.False(string.IsNullOrEmpty(err.GetString()));
	}

	[Fact]
	public async Task Happy_Path_Transfers_And_Returns_Updated_Owner()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 4004);
		var created = await owner.CreateGuildAsync("handover");
		var id = long.Parse(created.GetProperty("id").GetString()!);

		// seed a second member directly via the DbContext: no join endpoint
		// exists in #54, so we bypass the wire here on purpose
		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<GuildDbContext>();
			var memberResult = GuildMember.Create(id, userId: 4005, now: DateTimeOffset.UtcNow);
			Assert.True(memberResult.Succeeded);
			db.Members.Add(memberResult.Value);
			await db.SaveChangesAsync();
		}

		var resp = await owner.PatchAsJsonAsync(
			$"/v1/guilds/{id}/owner",
			new { new_owner_id = "4005" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var root = await resp.ReadJsonAsync();
		Assert.Equal("4005", root.GetProperty("owner_id").GetString());
	}
}
