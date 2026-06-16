using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class ReorderRolesTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.PatchAsJsonAsync("/v1/guilds/1/roles",
			new[] { new { id = "1", position = 1 } });
		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownGuild_Returns_404()
	{
		var client = factory.CreateAuthenticatedClient(userId: 9301);
		var resp = await client.PatchAsJsonAsync("/v1/guilds/9999999/roles",
			new[] { new { id = "1", position = 1 } });
		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task WithoutManageRoles_Returns_403()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9302);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		await factory.AddBareMemberAsync(id, userId: 9303);

		var member = factory.CreateAuthenticatedClient(userId: 9303);
		var resp = await member.PatchAsJsonAsync($"/v1/guilds/{id}/roles",
			new[] { new { id = "1", position = 1 } });

		Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
	}

	[Fact]
	public async Task HappyPath_Returns_200_AndSwapsRoles()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9304);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		await owner.PostAsJsonAsync($"/v1/guilds/{id}/roles", new { name = "A" });
		await owner.PostAsJsonAsync($"/v1/guilds/{id}/roles", new { name = "B" });

		// roles by position asc: [Administrator(1), A(2), B(3)] -> swap A and B
		var roles = await NonDefaultRolesByPositionAsync(owner, id);
		var (aId, aPos) = roles[^2];
		var (bId, bPos) = roles[^1];

		var resp = await owner.PatchAsJsonAsync($"/v1/guilds/{id}/roles",
			new[]
			{
				new { id = aId, position = bPos },
				new { id = bId, position = aPos },
			}, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
		var items = (await resp.ReadJsonAsync()).EnumerateArray().ToList();
		// response is ordered by position descending; @everyone (pos 0) last
		var positions = items.Select(i => i.GetProperty("position").GetInt32()).ToList();
		Assert.Equal(positions.OrderByDescending(p => p).ToList(), positions);
		Assert.Equal(0, positions[^1]);
		// A and B swapped slots
		Assert.Equal(bPos, items.Single(i => i.GetProperty("id").GetString() == aId).GetProperty("position").GetInt32());
		Assert.Equal(aPos, items.Single(i => i.GetProperty("id").GetString() == bId).GetProperty("position").GetInt32());
	}

	[Fact]
	public async Task DuplicateId_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9305);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;
		var roles = await NonDefaultRolesByPositionAsync(owner, id);

		var resp = await owner.PatchAsJsonAsync($"/v1/guilds/{id}/roles",
			new[]
			{
				new { id = roles[0].Id, position = 1 },
				new { id = roles[0].Id, position = 2 },
			}, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task EveryoneInBody_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9306);
		var guild = await owner.CreateGuildAsync("g");
		var id = long.Parse(guild.GetProperty("id").GetString()!);
		var everyoneId = await factory.GetEveryoneRoleIdAsync(id);
		var roles = await NonDefaultRolesByPositionAsync(owner, id.ToString());

		var resp = await owner.PatchAsJsonAsync($"/v1/guilds/{id}/roles",
			new[]
			{
				new { id = everyoneId.ToString(), position = roles[0].Position },
				new { id = roles[0].Id, position = 0 },
			}, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task NotAPermutation_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9307);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;
		var roles = await NonDefaultRolesByPositionAsync(owner, id);

		// move a single role to an unoccupied slot -> not a permutation of {its pos}
		var resp = await owner.PatchAsJsonAsync($"/v1/guilds/{id}/roles",
			new[] { new { id = roles[0].Id, position = 99 } }, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	[Fact]
	public async Task UnknownRoleId_Returns_404()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9308);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.PatchAsJsonAsync($"/v1/guilds/{id}/roles",
			new[] { new { id = "9999999", position = 1 } }, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
	}

	[Fact]
	public async Task NonNumericId_Returns_400()
	{
		var owner = factory.CreateAuthenticatedClient(userId: 9309);
		var guild = await owner.CreateGuildAsync("g");
		var id = guild.GetProperty("id").GetString()!;

		var resp = await owner.PatchAsJsonAsync($"/v1/guilds/{id}/roles",
			new[] { new { id = "not-a-snowflake", position = 1 } }, JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}

	// non-default roles (everything except @everyone) as (id, position) ordered by position asc
	private static async Task<List<(string Id, int Position)>> NonDefaultRolesByPositionAsync(
		HttpClient client, string guildId)
	{
		var list = await (await client.GetAsync($"/v1/guilds/{guildId}/roles")).ReadJsonAsync();
		return list.EnumerateArray()
			.Where(r => !r.GetProperty("is_default").GetBoolean())
			.OrderBy(r => r.GetProperty("position").GetInt32())
			.Select(r => (r.GetProperty("id").GetString()!, r.GetProperty("position").GetInt32()))
			.ToList();
	}
}
