using Xunit;

namespace Guild.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_DoesNotReference_Application()
    {
        Assert.False(Assemblies.Domain.References(Assemblies.Application),
            "Domain must not depend on Application.");
    }

    [Fact]
    public void Domain_DoesNotReference_Infrastructure()
    {
        Assert.False(Assemblies.Domain.References(Assemblies.Infrastructure),
            "Domain must not depend on Infrastructure.");
    }

    [Fact]
    public void Domain_DoesNotReference_Persistence()
    {
        Assert.False(Assemblies.Domain.References(Assemblies.Persistence),
            "Domain must not depend on Persistence.");
    }

    [Fact]
    public void Application_DoesNotReference_Infrastructure()
    {
        Assert.False(Assemblies.Application.References(Assemblies.Infrastructure),
            "Application must not depend on Infrastructure.");
    }

    [Fact]
    public void Application_DoesNotReference_Persistence()
    {
        Assert.False(Assemblies.Application.References(Assemblies.Persistence),
            "Application must not depend on Persistence.");
    }

    [Fact]
    public void Infrastructure_DoesNotReference_Persistence()
    {
        Assert.False(Assemblies.Infrastructure.References(Assemblies.Persistence),
            "Infrastructure must not depend on Persistence.");
    }

    [Fact]
    public void Persistence_DoesNotReference_Infrastructure()
    {
        Assert.False(Assemblies.Persistence.References(Assemblies.Infrastructure),
            "Persistence must not depend on Infrastructure.");
    }
}