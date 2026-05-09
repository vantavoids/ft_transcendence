using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace Chat.ArchitectureTests;

public sealed class HubConventionTests
{
	private static IEnumerable<Type> HubsIn(Assembly assembly) =>
		assembly.GetTypes()
			.Where(t => !t.IsAbstract && typeof(Hub).IsAssignableFrom(t));

	[Fact]
	public void AllHubs_MustBe_Sealed()
	{
		var violations = HubsIn(Assemblies.Presentation)
			.Where(t => !t.IsSealed)
			.Select(t => t.FullName!)
			.ToList();

		Assert.True(violations.Count == 0,
			"The following hubs are not sealed:\n  - " +
			string.Join("\n  - ", violations));
	}

	[Fact]
	public void AllHubs_MustResideIn_PresentationAssembly()
	{
		var forbiddenAssemblies = new[]
		{
			Assemblies.Domain,
			Assemblies.Application,
			Assemblies.Infrastructure,
			Assemblies.Persistence
		};

		var violations = forbiddenAssemblies
			.SelectMany(HubsIn)
			.Select(t => $"{t.Assembly.GetName().Name}: {t.FullName}")
			.ToList();

		Assert.True(violations.Count == 0,
			"The following hubs were found outside Chat.Presentation:\n  - " +
			string.Join("\n  - ", violations));
	}

	[Fact]
	public void AllHubs_MustBe_StronglyTyped()
	{
		var violations = HubsIn(Assemblies.Presentation)
			.Where(t => t.BaseType == typeof(Hub))
			.Select(t => t.FullName!)
			.ToList();

		Assert.True(violations.Count == 0,
			"The following hubs inherit Hub directly instead of Hub<TClient>:\n  - " +
			string.Join("\n  - ", violations));
	}
}
