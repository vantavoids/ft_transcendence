using System.Net.Http.Headers;
using System.Reflection;
using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Users;
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

				["UserService:BaseUrl"] = "http://user.test",
			});
		});

		builder.ConfigureTestServices(services =>
		{
			ReplaceDbContext(services);
			StripMassTransit(services);

			// re-register IEventBus with a no-op - handlers receive it via DI so
			// the binding must exist even when nothing observes the published events
			services.RemoveAll<IEventBus>();
			services.AddSingleton<IEventBus, NoopEventBus>();

			// User Service is mocked: the real client would try to dial over the
			// docker network. NoopUserService treats every user as existing and
			// returns a deterministic summary, matching graceful-degrade behaviour
			services.RemoveAll<IUserService>();
			services.AddSingleton<IUserService, NoopUserService>();
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

	/// <summary>
	/// inserts a Channel directly via the scoped DbContext for tests that need a
	/// pre-seeded channel. <c>channelId</c> is generated by snowflake when omitted
	/// (callers usually want to assert against it, so this returns it)
	/// </summary>
	public async Task<long> AddChannelAsync(
		long guildId,
		long? categoryId,
		string name,
		long? channelId = null)
	{
		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<GuildDbContext>();
		var ids = scope.ServiceProvider.GetRequiredService<Guild.Application.Abstractions.IIdGenerator>();
		var id = channelId ?? ids.NextId();
		var channelResult = Guild.Domain.Guild.Channel.Create(
			id: id,
			guildId: guildId,
			categoryId: categoryId,
			name: name,
			topic: null,
			type: Guild.Domain.Guild.ChannelType.Text,
			position: 0,
			now: DateTimeOffset.UtcNow);
		if (channelResult.IsFailure)
			throw new InvalidOperationException(channelResult.Error.Message);
		db.Channels.Add(channelResult.Value);
		await db.SaveChangesAsync();
		return id;
	}

	/// <summary>
	/// pulls the seeded @everyone role id for a guild so PUT-overwrite tests can
	/// target a role that actually exists. since CreateGuild auto-creates an
	/// @everyone + Administrator role pair under snowflake ids, the test cannot
	/// predict them; this seam reads them via the same DbContext the API uses
	/// </summary>
	public async Task<long> GetEveryoneRoleIdAsync(long guildId)
	{
		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<GuildDbContext>();
		var everyone = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
			.FirstAsync(db.Roles, r => r.GuildId == guildId && r.IsDefault);
		return everyone.Id;
	}

	public async Task AddChannelOverwriteAsync(
		long channelId,
		Guild.Domain.Guild.OverwriteTargetType type,
		long targetId,
		long allow,
		long deny)
	{
		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<GuildDbContext>();
		var ids = scope.ServiceProvider.GetRequiredService<Guild.Application.Abstractions.IIdGenerator>();
		var channel = await db.Channels.FindAsync(channelId)
			?? throw new InvalidOperationException($"Channel {channelId} not found");
		var owResult = Guild.Domain.Guild.ChannelPermissionOverwrite.Create(
			id: ids.NextId(),
			guildId: channel.GuildId,
			channelId: channelId,
			targetType: type,
			targetId: targetId,
			allow: allow,
			deny: deny,
			now: DateTimeOffset.UtcNow);
		if (owResult.IsFailure)
			throw new InvalidOperationException(owResult.Error.Message);
		db.ChannelPermissionOverwrites.Add(owResult.Value);
		await db.SaveChangesAsync();
	}

	/// <summary>
	/// inserts a GuildMember with a custom role granting the given permission bits.
	/// used to test permission-gated endpoints without going through an endpoint
	/// that does not yet exist (member invite / role assignment)
	/// </summary>
	public async Task AddMemberWithPermissionsAsync(long guildId, long userId, long permissions)
	{
		const BindingFlags AnyInstance =
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<GuildDbContext>();
		var ids = scope.ServiceProvider.GetRequiredService<IIdGenerator>();
		var now = DateTimeOffset.UtcNow;

		var memberResult = Guild.Domain.Guild.GuildMember.Create(guildId, userId, now);
		if (memberResult.IsFailure)
			throw new InvalidOperationException(memberResult.Error.Message);
		db.Members.Add(memberResult.Value);

		var roleResult = Guild.Domain.Guild.Role.Create(
			id: ids.NextId(), guildId: guildId, name: "TestRole", color: null,
			permissions: permissions, position: 99,
			isDefault: false, isHoisted: false, isMentionable: false, now: now);
		if (roleResult.IsFailure)
			throw new InvalidOperationException(roleResult.Error.Message);
		db.Roles.Add(roleResult.Value);
		await db.SaveChangesAsync();

		var memberRole = (Guild.Domain.Guild.MemberRole)Activator.CreateInstance(
			typeof(Guild.Domain.Guild.MemberRole), AnyInstance,
			binder: null, args: null, culture: null)!;
		typeof(Guild.Domain.Guild.MemberRole)
			.GetProperty(nameof(Guild.Domain.Guild.MemberRole.GuildId), AnyInstance)!
			.SetValue(memberRole, guildId);
		typeof(Guild.Domain.Guild.MemberRole)
			.GetProperty(nameof(Guild.Domain.Guild.MemberRole.UserId), AnyInstance)!
			.SetValue(memberRole, userId);
		typeof(Guild.Domain.Guild.MemberRole)
			.GetProperty(nameof(Guild.Domain.Guild.MemberRole.RoleId), AnyInstance)!
			.SetValue(memberRole, roleResult.Value.Id);
		typeof(Guild.Domain.Guild.MemberRole)
			.GetProperty(nameof(Guild.Domain.Guild.MemberRole.AssignedAt), AnyInstance)!
			.SetValue(memberRole, now);
		db.MemberRoles.Add(memberRole);
		await db.SaveChangesAsync();
	}

	/// <summary>
	/// seeds a <c>GuildBan</c> directly through the scoped DbContext. used by
	/// list-bans tests that need pre-existing rows without going through the
	/// ban endpoint (which lives in its own commit and is not yet wired up).
	/// </summary>
	public async Task AddBanAsync(long guildId, long userId, long bannedBy, string? reason = null)
	{
		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<GuildDbContext>();
		var banResult = Guild.Domain.Guild.GuildBan.Create(
			guildId: guildId,
			userId: userId,
			bannedBy: bannedBy,
			reason: reason,
			now: DateTimeOffset.UtcNow);
		if (banResult.IsFailure)
			throw new InvalidOperationException(banResult.Error.Message);
		db.GuildBans.Add(banResult.Value);
		await db.SaveChangesAsync();
	}

	/// <summary>
	/// seeds an active <c>GuildInvite</c> with no expiry directly through the scoped
	/// DbContext. used by join / invite tests that need an exact code to hit
	/// </summary>
	public async Task AddInviteAsync(long guildId, string code, long createdBy, int? maxUses = null)
	{
		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<GuildDbContext>();
		var inviteResult = Guild.Domain.Guild.GuildInvite.Create(
			code: code,
			guildId: guildId,
			createdBy: createdBy,
			maxUses: maxUses,
			expiresAt: null,
			now: DateTimeOffset.UtcNow);
		if (inviteResult.IsFailure)
			throw new InvalidOperationException(inviteResult.Error.Message);
		db.GuildInvites.Add(inviteResult.Value);
		await db.SaveChangesAsync();
	}

	private sealed class NoopEventBus : IEventBus
	{
		public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
			where T : class => Task.CompletedTask;
	}

	private sealed class NoopUserService : IUserService
	{
		public Task<bool> ExistsAsync(long userId, CancellationToken cancellationToken = default)
			=> Task.FromResult(true);

		public Task<UserSummary?> GetSummaryAsync(long userId, CancellationToken cancellationToken = default)
			=> Task.FromResult<UserSummary?>(new UserSummary(userId, $"user{userId}"));
	}
}
