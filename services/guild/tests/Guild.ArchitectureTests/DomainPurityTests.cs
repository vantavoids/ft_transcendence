using Xunit;

namespace Guild.ArchitectureTests;

public sealed class DomainPurityTests
{
    private static readonly string[] DomainForbiddenPrefixes =
    [
        "MassTransit",
        "Carter",
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Extensions",
        "System.ComponentModel.DataAnnotations"
    ];

    private static readonly string[] ApplicationForbiddenPrefixes =
    [
        "MassTransit",
        "Carter",
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "MongoDB"
    ];

    [Fact]
    public void Domain_DoesNotReference_AnyFrameworkAssemblies()
    {
        var violations = Assemblies.Domain
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => DomainForbiddenPrefixes.Any(prefix =>
                name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        Assert.True(violations.Count == 0,
            "Domain references framework assemblies it must not depend on:\n  - " +
            string.Join("\n  - ", violations));
    }

    [Fact]
    public void Application_DoesNotReference_InfrastructureFrameworks()
    {
        var violations = Assemblies.Application
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => ApplicationForbiddenPrefixes.Any(prefix =>
                name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        Assert.True(violations.Count == 0,
            "Application references infrastructure-specific assemblies it must not depend on:\n  - " +
            string.Join("\n  - ", violations));
    }
}