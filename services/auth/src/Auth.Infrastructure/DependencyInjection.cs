using System.Reflection;
using Auth.Application.Abstractions;
using Auth.Application.Abstractions.Events;
using Auth.Application.Abstractions.OAuth;
using Auth.Application.Abstractions.Security;
using Auth.Infrastructure.OAuth;
using Auth.Infrastructure.Options;
using Auth.Infrastructure.Security;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        SetupFromAssembly(services,
            t => typeof(IOptions).IsAssignableFrom(t),
            nameof(ConfigureOptions));

        SetupFromAssembly(services,
            t => typeof(IOAuthProviderClient).IsAssignableFrom(t) &&
                t.GetCustomAttribute<OAuthProviderKeyAttribute>() is not null,
            nameof(RegisterOAuthProvider));

        services.AddMassTransit(x =>
            x.UsingRabbitMq((ctx, conf) => {
                var options = ctx.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                conf.Host(options.Host, (ushort)options.Port, options.VirtualHost, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });
                conf.ConfigureEndpoints(ctx);
            })
        );

        services.AddHttpContextAccessor();
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, SnowflakeIdGenerator>();
        services.AddSingleton<ISecretHasher, SecretHasher>();
        services.AddSingleton<ITokenGenerator, TokenGenerator>();
        services.AddTransient<ICurrentUser, CurrentUser>();

        return services;
    }

    private static void SetupFromAssembly(
        IServiceCollection services,
        Func<Type, bool> filter,
        string methodName)
    {
        var method = typeof(DependencyInjection)
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!;

        foreach (var type in typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && filter(t)))
        {
            method.MakeGenericMethod(type).Invoke(null, [services]);
        }
    }

    private static void ConfigureOptions<T>(IServiceCollection services)
        where T : class, IOptions
    {
        services.AddOptions<T>()
            .BindConfiguration(T.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static void RegisterOAuthProvider<T>(IServiceCollection services)
        where T : class, IOAuthProviderClient
    {
        var key = typeof(T).GetCustomAttribute<OAuthProviderKeyAttribute>()!.Provider;
        services.AddHttpClient<T>();
        services.AddKeyedTransient<IOAuthProviderClient, T>(key);
    }
}
