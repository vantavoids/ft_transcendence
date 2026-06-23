using Auth.Application.Abstractions.Persistence;
using Auth.Persistence.Db;
using Auth.Persistence.Health;
using Auth.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Auth.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddOptions<DbOptions>()
            .BindConfiguration(DbOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<AuthDbContext>((ctx, config) => {
            var options = ctx.GetRequiredService<IOptions<DbOptions>>().Value;
            config.UseNpgsql(options.ToConnectionString());
        });

        services.AddScoped<IAuthUserRepository, AuthUserRepository>();

        return services;
    }

    public static IHealthChecksBuilder AddPersistenceHealthChecks(this IHealthChecksBuilder builder)
        => builder.AddCheck<EfCoreHealthCheck>("postgres");
}
