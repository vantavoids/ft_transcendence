using Microsoft.Extensions.DependencyInjection;

namespace Auth.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services) => services;
}