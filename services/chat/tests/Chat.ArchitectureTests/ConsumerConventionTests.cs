using System.Reflection;
using MassTransit;
using Xunit;

namespace Chat.ArchitectureTests;

public sealed class ConsumerConventionTests
{
	private static IEnumerable<Type> ConsumersIn(Assembly assembly) =>
		assembly.GetTypes()
			.Where(t => !t.IsAbstract && !t.IsInterface)
			.Where(t => t.GetInterfaces()
				.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>)));

	[Fact]
	public void AllConsumers_MustBe_Sealed()
	{
		var allAssemblies = new[]
		{
			Assemblies.Domain,
			Assemblies.Application,
			Assemblies.Infrastructure,
			Assemblies.Persistence,
			Assemblies.Presentation
		};

		var violations = allAssemblies
			.SelectMany(ConsumersIn)
			.Where(t => !t.IsSealed)
			.Select(t => $"{t.Assembly.GetName().Name}: {t.FullName}")
			.ToList();

		Assert.True(violations.Count == 0,
			"The following consumers are not sealed:\n  - " +
			string.Join("\n  - ", violations));
	}

	[Fact]
	public void AllConsumers_MustResideIn_InfrastructureAssembly()
	{
		var forbiddenAssemblies = new[]
		{
			Assemblies.Domain,
			Assemblies.Application,
			Assemblies.Persistence,
			Assemblies.Presentation
		};

		var violations = forbiddenAssemblies
			.SelectMany(ConsumersIn)
			.Select(t => $"{t.Assembly.GetName().Name}: {t.FullName}")
			.ToList();

		Assert.True(violations.Count == 0,
			"The following consumers were found outside Chat.Infrastructure:\n  - " +
			string.Join("\n  - ", violations));
	}
}
