using System.Reflection;
using Guild.Application.Abstractions.Messaging;
using Xunit;

namespace Guild.ArchitectureTests;

public sealed class HandlerConventionTests
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(IQueryHandler<,>),
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>)
    ];

    private static IEnumerable<Type> HandlersIn(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType &&
                          HandlerInterfaces.Contains(i.GetGenericTypeDefinition())));
    }

    [Fact]
    public void AllHandlers_MustBe_InternalSealed()
    {
        var allAssemblies = new[]
        {
            Assemblies.Domain,
            Assemblies.Application,
            Assemblies.Infrastructure,
            Assemblies.Persistence
        };

        var violations = allAssemblies
            .SelectMany(HandlersIn)
            .Where(t => !(t.IsNotPublic && t.IsSealed))
            .Select(t => $"{t.Assembly.GetName().Name}: {t.FullName}")
            .ToList();

        Assert.True(violations.Count == 0,
            "The following handlers are not 'internal sealed':\n  - " +
            string.Join("\n  - ", violations));
    }

    [Fact]
    public void AllHandlers_MustResideIn_ApplicationAssembly()
    {
        var forbiddenAssemblies = new[]
        {
            Assemblies.Domain,
            Assemblies.Infrastructure,
            Assemblies.Persistence
        };

        var violations = forbiddenAssemblies
            .SelectMany(HandlersIn)
            .Select(t => $"{t.Assembly.GetName().Name}: {t.FullName}")
            .ToList();

        Assert.True(violations.Count == 0,
            "The following handlers were found outside Guild.Application:\n  - " +
            string.Join("\n  - ", violations));
    }
}