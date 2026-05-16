using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class CreateGuildTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	[Fact]
	public async Task Without_Token_Returns_401()
	{
		var client = factory.CreateClient();
		var resp = await client.PostAsJsonAsync(
			"/v1/guilds",
			new { name = "Hello" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
	}

	[Fact]
	public async Task Valid_Body_Returns_201_AndContractShape()
	{
		var client = factory.CreateAuthenticatedClient(userId: 42);
		var resp = await client.PostAsJsonAsync(
			"/v1/guilds",
			new { name = "Hello", description = "world" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

		var root = await resp.ReadJsonAsync();

		Assert.Equal(JsonValueKind.String, root.GetProperty("id").ValueKind);
		Assert.True(long.TryParse(root.GetProperty("id").GetString(), out _));

		Assert.Equal("Hello", root.GetProperty("name").GetString());
		Assert.Equal("world", root.GetProperty("description").GetString());
		Assert.Equal("42", root.GetProperty("owner_id").GetString());
		Assert.True(root.TryGetProperty("created_at", out _));

		Assert.NotNull(resp.Headers.Location);
		var id = root.GetProperty("id").GetString()!;
		Assert.Contains($"/v1/guilds/{id}", resp.Headers.Location!.ToString());
	}

	[Fact]
	public async Task Missing_Name_Returns_400_WithErrorBody()
	{
		var client = factory.CreateAuthenticatedClient(userId: 42);
		var resp = await client.PostAsJsonAsync(
			"/v1/guilds",
			new { description = "no name here :p" },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

		var root = await resp.ReadJsonAsync();
		Assert.True(root.TryGetProperty("error", out var err));
		Assert.False(string.IsNullOrEmpty(err.GetString()));
	}

	[Fact]
	public async Task Name_Over_100_Chars_Returns_400()
	{
		var client = factory.CreateAuthenticatedClient(userId: 42);
		var resp = await client.PostAsJsonAsync(
			"/v1/guilds",
			new { name = new string('A', 101) },
			JsonOptions.SnakeCase);

		Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
	}
}
