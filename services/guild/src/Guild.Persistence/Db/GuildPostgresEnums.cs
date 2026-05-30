using System.Reflection;
using Guild.Domain.Guild;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Guild.Persistence.Db;

/// <summary>
/// single source of truth for the postgres enums owned by this service. both
/// extension methods iterate <see cref="Types"/> via reflection so the runtime
/// parameter mapping and the model registration cannot drift apart. omit either
/// and EF emits CLR enums as integers, which PG rejects with 42804 (type mismatch)
/// </summary>
internal static class GuildPostgresEnums
{
	private static readonly Type[] Types = [typeof(ChannelType)];

	// nulls in the args array yield Npgsql's default snake_case translator, which
	// matches the labels HasPostgresEnum<T>() generates
	private static readonly MethodInfo MapEnumGeneric = typeof(NpgsqlDbContextOptionsBuilder)
		.GetMethods(BindingFlags.Public | BindingFlags.Instance)
		.Single(m => m.Name == nameof(NpgsqlDbContextOptionsBuilder.MapEnum)
			&& m.IsGenericMethodDefinition
			&& m.GetGenericArguments().Length == 1);

	private static readonly MethodInfo HasPostgresEnumGeneric = typeof(NpgsqlModelBuilderExtensions)
		.GetMethods(BindingFlags.Public | BindingFlags.Static)
		.Single(m => m.Name == nameof(NpgsqlModelBuilderExtensions.HasPostgresEnum)
			&& m.IsGenericMethodDefinition
			&& m.GetGenericArguments().Length == 1
			&& m.GetParameters().Length > 0
			&& m.GetParameters()[0].ParameterType == typeof(ModelBuilder));

	public static NpgsqlDbContextOptionsBuilder MapPostgresEnums(this NpgsqlDbContextOptionsBuilder builder)
	{
		var args = new object?[MapEnumGeneric.GetParameters().Length];
		foreach (var t in Types)
			MapEnumGeneric.MakeGenericMethod(t).Invoke(builder, args);
		return builder;
	}

	public static ModelBuilder RegisterPostgresEnums(this ModelBuilder modelBuilder)
	{
		var args = new object?[HasPostgresEnumGeneric.GetParameters().Length];
		args[0] = modelBuilder;
		foreach (var t in Types)
			HasPostgresEnumGeneric.MakeGenericMethod(t).Invoke(null, args);
		return modelBuilder;
	}
}
