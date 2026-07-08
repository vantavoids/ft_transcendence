using System.Net;
using Auth.Application.Abstractions.Persistence;
using Auth.Domain.AuthUser;
using Auth.FunctionalTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Auth.FunctionalTests.Endpoints;

public sealed class UserDataExportEndpointTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
	[Fact]
	public async Task Anonymous_OnInternalGroup_NotUnderV1_RejectsNonPositive()
	{
		var anon = factory.CreateClient();
		// internal group: reachable without a token, but not under /v1 (gateway-facing)
		Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync("/internal/users/1/data-export")).StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync("/v1/users/1/data-export")).StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, (await anon.GetAsync("/internal/users/0/data-export")).StatusCode);
	}

	[Fact]
	public async Task UnknownUser_Returns_AllNullExport()
	{
		var anon = factory.CreateClient();
		var body = await (await anon.GetAsync("/internal/users/888888/data-export")).ReadJsonAsync();

		Assert.Equal("888888", body["user_id"]?.GetValue<string>());
		Assert.Null(body["email"]);
		Assert.Null(body["email_verified"]);
		Assert.Null(body["oauth_provider"]);
		Assert.Null(body["oauth_id"]);
		Assert.Null(body["created_at"]);
	}

	[Fact]
	public async Task EmailPasswordUser_Returns200_WithNullOAuthFields()
	{
		await factory.SeedEmailUserAsync("export_pw@example.com", "password123");
		var anon = factory.CreateClient();

		using var scope = factory.Services.CreateScope();
		var users = scope.ServiceProvider.GetRequiredService<IAuthUserRepository>();
		var user = await users.GetByEmailAsync("export_pw@example.com");

		var body = await (await anon.GetAsync($"/internal/users/{user!.Id}/data-export")).ReadJsonAsync();

		Assert.Equal(user.Id.ToString(), body["user_id"]?.GetValue<string>());
		Assert.Equal("export_pw@example.com", body["email"]?.GetValue<string>());
		Assert.False(body["email_verified"]?.GetValue<bool>());
		Assert.Null(body["oauth_provider"]);
		Assert.Null(body["oauth_id"]);
		Assert.NotNull(body["created_at"]?.GetValue<DateTimeOffset>());
	}

	[Fact]
	public async Task OAuthUser_Returns200_WithProviderAndNullEmail()
	{
		_ = await factory.SeedOAuthUserWithAccessTokenAsync(OAuthProvider.FortyTwo, "42-export-123");
		var anon = factory.CreateClient();

		using var scope = factory.Services.CreateScope();
		var users = scope.ServiceProvider.GetRequiredService<IAuthUserRepository>();
		var user = await users.GetByOAuthAsync(OAuthProvider.FortyTwo, "42-export-123");

		var body = await (await anon.GetAsync($"/internal/users/{user!.Id}/data-export")).ReadJsonAsync();

		Assert.Null(body["email"]);
		Assert.Null(body["email_verified"]);
		Assert.Equal("fortytwo", body["oauth_provider"]?.GetValue<string>());
		Assert.Equal("42-export-123", body["oauth_id"]?.GetValue<string>());
		Assert.NotNull(body["created_at"]?.GetValue<DateTimeOffset>());
	}
}
