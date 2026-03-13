using System.Reflection;
using Guild.Application;
using Guild.Domain;
using Guild.Infrastructure;
using Guild.Persistence;
using Guild.Presentation;

namespace Guild.ArchitectureTests;

internal static class Assemblies
{
    internal static readonly Assembly Domain         = typeof(IDomainAssemblyMarker).Assembly;
    internal static readonly Assembly Application    = typeof(IApplicationAssemblyMarker).Assembly;
    internal static readonly Assembly Infrastructure = typeof(IInfrastructureAssemblyMarker).Assembly;
    internal static readonly Assembly Persistence    = typeof(IPersistenceAssemblyMarker).Assembly;
    internal static readonly Assembly Presentation   = typeof(IPresentationAssemblyMarker).Assembly;

    internal static bool References(this Assembly subject, Assembly target)
    {
        return subject.GetReferencedAssemblies()
            .Any(r => string.Equals(r.Name, target.GetName().Name, StringComparison.Ordinal));
    }
}