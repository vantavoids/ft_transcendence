using Auth.Application.Abstractions;
using Auth.Application.Abstractions.Events;
using Auth.Application.Abstractions.OAuth;
using Auth.Application.Abstractions.Persistence;
using Auth.Application.Abstractions.Security;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Auth.Persistence.Db;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Auth.FunctionalTests.Infrastructure;

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    public const string JwtSecret =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    static AuthApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__SecretKey", JwtSecret);
        Environment.SetEnvironmentVariable("POSTGRES_USER", "test");
        Environment.SetEnvironmentVariable("POSTGRES_PASSWORD", "test");
    }

    private readonly string _dbName = "auth-tests-" + Guid.NewGuid();

    public FakeOAuthProviderClient OAuthClient { get; } = new();

    public async Task SeedEmailUserAsync(string email, string rawPassword)
    {
        using var scope = Services.CreateScope();
        var sp     = scope.ServiceProvider;
        var repo   = sp.GetRequiredService<IAuthUserRepository>();
        var hasher = sp.GetRequiredService<ISecretHasher>();
        var clock  = sp.GetRequiredService<IClock>();
        var idGen  = sp.GetRequiredService<IIdGenerator>();

        var user = AuthUser.CreateEmailPasswordUser(
            id: idGen.NextId(),
            email: email,
            passwordHash: hasher.Hash(rawPassword),
            now: clock.UtcNow).Value;

        await repo.AddAsync(user);
        await repo.SaveChangesAsync();
    }

    public async Task<string> SeedUserWithRefreshTokenAsync(string email, string rawPassword)
    {
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        var repo = sp.GetRequiredService<IAuthUserRepository>();
        var hasher = sp.GetRequiredService<ISecretHasher>();
        var clock = sp.GetRequiredService<IClock>();
        var idGen = sp.GetRequiredService<IIdGenerator>();
        var tokenGen = sp.GetRequiredService<ITokenGenerator>();

        var rawRefreshToken = tokenGen.GenerateRefreshToken();
        var now = clock.UtcNow;

        var user = AuthUser.CreateEmailPasswordUser(
            id: idGen.NextId(),
            email: email,
            passwordHash: hasher.Hash(rawPassword),
            now: now).Value;

        user.SetRefreshToken(
            hasher.HashDeterministic(rawRefreshToken),
            now,
            now.Add(tokenGen.GetRefreshTokenLifetime()));

        await repo.AddAsync(user);
        await repo.SaveChangesAsync();

        return rawRefreshToken;
    }

    public async Task<string> SeedUserWithAccessTokenAsync(string email, string rawPassword)
    {
        using var scope = Services.CreateScope();
        var sp          = scope.ServiceProvider;
        var repo        = sp.GetRequiredService<IAuthUserRepository>();
        var hasher      = sp.GetRequiredService<ISecretHasher>();
        var clock       = sp.GetRequiredService<IClock>();
        var idGen       = sp.GetRequiredService<IIdGenerator>();
        var tokenGen    = sp.GetRequiredService<ITokenGenerator>();

        var user = AuthUser.CreateEmailPasswordUser(
            id: idGen.NextId(),
            email: email,
            passwordHash: hasher.Hash(rawPassword),
            now: clock.UtcNow).Value;

        await repo.AddAsync(user);
        await repo.SaveChangesAsync();

        return tokenGen.GenerateAccessToken(user.Id);
    }

    public async Task<(string AccessToken, long UserId)> SeedUserWithBothTokensAsync(
        string email, string rawPassword)
    {
        using var scope = Services.CreateScope();
        var sp          = scope.ServiceProvider;
        var repo        = sp.GetRequiredService<IAuthUserRepository>();
        var hasher      = sp.GetRequiredService<ISecretHasher>();
        var clock       = sp.GetRequiredService<IClock>();
        var idGen       = sp.GetRequiredService<IIdGenerator>();
        var tokenGen    = sp.GetRequiredService<ITokenGenerator>();

        var rawRefreshToken = tokenGen.GenerateRefreshToken();
        var now             = clock.UtcNow;

        var user = AuthUser.CreateEmailPasswordUser(
            id: idGen.NextId(),
            email: email,
            passwordHash: hasher.Hash(rawPassword),
            now: now).Value;

        user.SetRefreshToken(
            hasher.HashDeterministic(rawRefreshToken),
            now,
            now.Add(tokenGen.GetRefreshTokenLifetime()));

        await repo.AddAsync(user);
        await repo.SaveChangesAsync();

        return (tokenGen.GenerateAccessToken(user.Id), user.Id);
    }

    public async Task<bool> IsRefreshTokenRevokedAsync(long userId)
    {
        using var scope = Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAuthUserRepository>();
        var user = await repo.GetByIdAsync(userId);
        return user?.RefreshToken?.Revoked ?? false;
    }

    public async Task<string> SeedOAuthUserWithAccessTokenAsync(OAuthProvider provider, string oauthId)
    {
        using var scope = Services.CreateScope();
        var sp       = scope.ServiceProvider;
        var repo     = sp.GetRequiredService<IAuthUserRepository>();
        var clock    = sp.GetRequiredService<IClock>();
        var idGen    = sp.GetRequiredService<IIdGenerator>();
        var tokenGen = sp.GetRequiredService<ITokenGenerator>();

        var user = AuthUser.CreateOAuthUser(
            id: idGen.NextId(),
            oauthProvider: provider,
            oauthId: oauthId,
            now: clock.UtcNow).Value;

        await repo.AddAsync(user);
        await repo.SaveChangesAsync();

        return tokenGen.GenerateAccessToken(user.Id);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:BaseUrl"] = "https://app.test",

                ["Database:Host"]     = "test",
                ["Database:Port"]     = "5432",
                ["Database:Name"]     = "test",
                ["Database:User"]     = "test",
                ["Database:Password"] = "test",

                ["Jwt:SecretKey"]  = JwtSecret,
                ["Jwt:Issuer"]     = "auth-service",
                ["Jwt:Audience"]   = "transcendence-api",

                ["RabbitMq:Host"]        = "localhost",
                ["RabbitMq:VirtualHost"] = "/",
                ["RabbitMq:Username"]    = "test",
                ["RabbitMq:Password"]    = "test",

                ["RefreshToken:TtlDays"]    = "7",
                ["RefreshToken:ByteLength"] = "64",
                ["RefreshToken:CookieName"] = "refresh_token",

                ["OAuthStateCookie:CookieName"] = "oauth_state",
                ["OAuthStateCookie:TtlMinutes"] = "10",

                ["OAuth:FortyTwo:ClientId"]     = "test-client-id",
                ["OAuth:FortyTwo:ClientSecret"] = "test-client-secret",
                ["OAuth:FortyTwo:RedirectUri"]  = "https://app.test/callback",
                ["OAuth:Google:ClientId"]       = "test-client-id",
                ["OAuth:Google:ClientSecret"]   = "test-client-secret",
                ["OAuth:Google:RedirectUri"]    = "https://app.test/callback",
                ["OAuth:Github:ClientId"]       = "test-client-id",
                ["OAuth:Github:ClientSecret"]   = "test-client-secret",
                ["OAuth:Github:RedirectUri"]    = "https://app.test/callback",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            ReplaceDbContext(services);
            StripMassTransit(services);

            services.RemoveAll<IEventBus>();
            services.AddSingleton<IEventBus, NoopEventBus>();

            foreach (var provider in new[] { OAuthProvider.FortyTwo, OAuthProvider.Google, OAuthProvider.Github })
                services.AddKeyedSingleton<IOAuthProviderClient>(provider, OAuthClient);
        });
    }

    private void ReplaceDbContext(IServiceCollection services)
    {
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

        services.RemoveAll<AuthDbContext>();
        services.AddDbContext<AuthDbContext>(o => o.UseInMemoryDatabase(_dbName));
    }

    private static void StripMassTransit(IServiceCollection services)
    {
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

    private sealed class NoopEventBus : IEventBus
    {
        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class, IEvent => Task.CompletedTask;
    }
}

public sealed class FakeOAuthProviderClient : IOAuthProviderClient
{
    private Result<OAuthUserInfo> _exchangeResult =
        new OAuthUserInfo("fake-provider-id", "fake@example.com", true);

    public Uri BuildAuthorizationUrl(string state)
        => new($"https://fake-provider.example.com/oauth/authorize?state={state}");

    public Task<Result<OAuthUserInfo>> ExchangeCodeAsync(
        string code, string state, CancellationToken cancellationToken = default)
        => Task.FromResult(_exchangeResult);

    public void SetupSuccess(OAuthUserInfo info) => _exchangeResult = info;

    public void SetupFailure(Failure failure) => _exchangeResult = failure;
}
