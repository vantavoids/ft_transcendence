using System.Reflection;
using System.Runtime.CompilerServices;
using Guild.Domain.Guild;
using Guild.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Guild.UnitTests.Persistence;

public sealed class GuildPostgresEnumsTests
{
	[Fact]
	public void StaticInit_Succeeds()
	{
		// the .Single() filters in GuildPostgresEnums throw at class init if Npgsql
		// renames or reshapes MapEnum<T> / HasPostgresEnum<T>. force the static
		// constructor so a package bump fails this test instead of crashing at startup
		var ex = Record.Exception(
			() => RuntimeHelpers.RunClassConstructor(typeof(GuildPostgresEnums).TypeHandle));
		Assert.Null(ex);
	}

	[Fact]
	public void Registry_ContainsAllDomainPostgresEnums()
	{
		var registry = GetRegistry();
		Assert.Contains(typeof(ChannelType), registry);
	}

	[Fact]
	public void Registry_OnlyHoldsEnumTypes()
	{
		var registry = GetRegistry();
		Assert.All(registry, t => Assert.True(t.IsEnum, $"{t.FullName} is not an enum"));
	}

	[Fact]
	public void MapPostgresEnums_InvokesWithoutThrowing()
	{
		var builder = new DbContextOptionsBuilder<DummyContext>();
		var ex = Record.Exception(
			() => builder.UseNpgsql("Host=localhost;Database=x;Username=x;Password=x",
				o => o.MapPostgresEnums()));
		Assert.Null(ex);
	}

	[Fact]
	public void RegisterPostgresEnums_InvokesWithoutThrowing()
	{
		var modelBuilder = new ModelBuilder();
		var ex = Record.Exception(() => modelBuilder.RegisterPostgresEnums());
		Assert.Null(ex);
	}

	private static Type[] GetRegistry()
	{
		var field = typeof(GuildPostgresEnums)
			.GetField("Types", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.NotNull(field);
		return (Type[])field!.GetValue(null)!;
	}

	private sealed class DummyContext(DbContextOptions<DummyContext> options) : DbContext(options) { }
}
