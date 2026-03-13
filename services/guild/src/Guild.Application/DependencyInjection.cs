using Guild.Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Guild.Application;

public static class DependencyInjection
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(IQueryHandler<,>),
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>)
    ];

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        foreach (var type in typeof(IApplicationAssemblyMarker).Assembly.GetTypes()
                     .Where(t => t is { IsAbstract: false, IsInterface: false }))
        foreach (var iface in type.GetInterfaces()
                     .Where(i => i.IsGenericType && HandlerInterfaces.Contains(i.GetGenericTypeDefinition())))
            services.AddScoped(iface, type);

        return services;
    }
}