using Microsoft.Extensions.DependencyInjection;

namespace Guild.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        return services;
    }
}