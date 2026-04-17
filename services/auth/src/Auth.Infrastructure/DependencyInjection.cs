using System.Reflection;
using Auth.Application.Abstractions;
using Auth.Application.Abstractions.Events;
using Auth.Application.Abstractions.Security;
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
        var optionsTypes = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } &&
                        typeof(IOptions).IsAssignableFrom(t));

        foreach (var optionType in optionsTypes)
        {
            var configureOptionsMethod = typeof(DependencyInjection)
                .GetMethod("ConfigureOptions", BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(optionType);

            configureOptionsMethod.Invoke(null, [services]);
        }

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

        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, SnowflakeIdGenerator>();
        services.AddSingleton<ISecretHasher, BcryptHasher>();
        services.AddSingleton<ITokenGenerator, TokenGenerator>();

        // services.AddKeyedTransient<IOAuthProviderClient, GithubOAuthProviderClient>(OAuthProvider.Github);
        // services.AddKeyedTransient<IOAuthProviderClient, GoogleOAuthProviderClient>(OAuthProvider.Google);
        // services.AddKeyedTransient<IOAuthProviderClient, FortyTwoOAuthProviderClient>(OAuthProvider.FortyTwo);

        return services;
    }

    private static OptionsBuilder<T> ConfigureOptions<T>(IServiceCollection services)
        where T : class, IOptions
    {
        return services.AddOptions<T>()
            .BindConfiguration(T.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
