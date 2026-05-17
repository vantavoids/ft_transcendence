using System.Net.Http.Headers;
using Guild.Application.Abstractions;
using Guild.Persistence.Db;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Guild.FunctionalTests.Infrastructure;

/// <summary>
/// boots the Guild Service against an in-memory EF store and strips MassTransit
/// out of DI so the test host never tries to dial RabbitMQ. each factory
/// instance gets its own in-memory database for isolation across test classes
/// </summary>
public sealed class GuildApiFactory : WebApplicationFactory<Program>
{
	// fixed 64-char secret so tokens minted by TestTokens validate against the
	// same key the JwtBearer middleware reads from configuration
	// and YES prod has much more secure secrets (hopefully)
	public const string JwtSecret =
		"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

	// Program.cs reads builder.Configuration["Jwt:SecretKey"] *eagerly* during
	// host startup, before ConfigureAppConfiguration callbacks fire. setting it
	// via env at process scope ensures CreateBuilder() picks it up. safe because
	// the value is a constant — every factory instance writes the same secret
	static GuildApiFactory()
	{
		Environment.SetEnvironmentVariable("Jwt__SecretKey", JwtSecret);
	}

	// unique per factory instance - IClassFixture<GuildApiFactory> reuses one
	// instance per test class, so tests within a class share state, but classes
	// don't bleed into each other
	private readonly string _dbName = "guild-tests-" + Guid.NewGuid();

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");

		builder.ConfigureAppConfiguration((_, config) =>
		{
			config.AddInMemoryCollection(new Dictionary<string, string?>
			{
				// DbContext is swapped below, but Options<DbOptions>.ValidateOnStart
				// fires before that, so the section still needs to bind cleanly
				["Database:Host"] = "test",
				["Database:Port"] = "5432",
				["Database:Name"] = "test",
				["Database:User"] = "test",
				["Database:Password"] = "test",

				["Jwt:SecretKey"] = JwtSecret,

				// MassTransit is stripped, but RabbitMqOptions still validates
				["RabbitMQ:Host"] = "localhost",
				["RabbitMQ:VirtualHost"] = "/",
				["RabbitMQ:Username"] = "test",
				["RabbitMQ:Password"] = "test",

				["Snowflake:WorkerId"] = "2",

				["BackendConfiguration:BaseUrl"] = "http://localhost",
				["BackendConfiguration:BaseApiUrl"] = "http://localhost/api",
			});
		});

		builder.ConfigureTestServices(services =>
		{
			ReplaceDbContext(services);
			StripMassTransit(services);

			// re-register IEventBus with a no-op - none of the #54 endpoints
			// publish, but handlers receive it via DI so the binding must exist
			services.RemoveAll<IEventBus>();
			services.AddSingleton<IEventBus, NoopEventBus>();
		});
	}

	private void ReplaceDbContext(IServiceCollection services)
	{
		// AddDbContext registers a configuration callback under an internal EF
		// type. if we decide to leave it alone, UseInMemoryDatabase stacks on top of
		// UseNpgsql and EF's "multiple providers" guard trips at resolve time.
		// easiest reliable purge: drop every EntityFrameworkCore- or Npgsql-
		// namespaced descriptor, then re-register against the in-memory provider
		var toRemove = services
			.Where(d =>
				(d.ServiceType.FullName ?? string.Empty)
					.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
				|| (d.ServiceType.FullName ?? string.Empty)
					.StartsWith("Npgsql", StringComparison.Ordinal)
				|| (d.ImplementationType?.FullName ?? string.Empty)
					.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
				|| (d.ImplementationType?.FullName ?? string.Empty)
					.StartsWith("Npgsql", StringComparison.Ordinal))
			.ToList();
		foreach (var d in toRemove)
			services.Remove(d);

		services.RemoveAll<GuildDbContext>();
		services.AddDbContext<GuildDbContext>(o => o.UseInMemoryDatabase(_dbName));
	}

	private static void StripMassTransit(IServiceCollection services)
	{
		// drop every descriptor whose service or implementation type sits in
		// the MassTransit namespace. catches IBus, IBusControl, the hosted
		// service, the registration context, etc
		var toRemove = services
			.Where(d =>
				(d.ServiceType.FullName ?? string.Empty)
					.StartsWith("MassTransit", StringComparison.Ordinal)
				|| (d.ImplementationType?.FullName ?? string.Empty)
					.StartsWith("MassTransit", StringComparison.Ordinal))
			.ToList();

		foreach (var d in toRemove)
			services.Remove(d);
	}

	public HttpClient CreateAuthenticatedClient(long userId)
	{
		var client = CreateClient();
		client.DefaultRequestHeaders.Authorization =
			new AuthenticationHeaderValue("Bearer", TestTokens.Issue(JwtSecret, userId));
		return client;
	}

	/// <summary>
	/// inserts a bare GuildMember row (no role assignments) directly via the
	/// scoped DbContext, so the test can exercise the "member without a
	/// permission-granting role" branch even though no public endpoint to add
	/// members exists yet. uses the Domain.Guild.GuildMember factory so the
	/// invariants stay enforced and the entity is then attached as Added
	/// </summary>
	public async Task AddBareMemberAsync(long guildId, long userId)
	{
		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<GuildDbContext>();
		var memberResult = Guild.Domain.Guild.GuildMember.Create(guildId, userId, DateTimeOffset.UtcNow);
		if (memberResult.IsFailure)
			throw new InvalidOperationException(memberResult.Error.Message);
		db.Members.Add(memberResult.Value);
		await db.SaveChangesAsync();
	}

	private sealed class NoopEventBus : IEventBus
	{
		public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
			where T : class => Task.CompletedTask;
	}
}
