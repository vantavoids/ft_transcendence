using Auth.Application.Abstractions;
using Auth.Application.Abstractions.Events;
using Auth.Application.Abstractions.Persistence;
using Auth.Application.Abstractions.Security;
using Auth.Domain.AuthUser;
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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
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
            });
        });

        builder.ConfigureTestServices(services =>
        {
            ReplaceDbContext(services);
            StripMassTransit(services);

            services.RemoveAll<IEventBus>();
            services.AddSingleton<IEventBus, NoopEventBus>();
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
