using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guild.Persistence.Db;

/// <summary>
/// adds optimistic-concurrency control backed by Postgres' <c>xmin</c> system
/// column. <c>xmin</c> holds the id of the transaction that last wrote a row and
/// Postgres bumps it on every UPDATE, so it works as a row version for free.
/// </summary>
/// <remarks>
/// uses the standard EF <c>IsRowVersion()</c> on a <c>uint</c> shadow property:
/// the Npgsql provider maps any <c>uint</c> row-version property to <c>xmin</c>
/// automatically (the PostgreSQL-specific <c>UseXminAsConcurrencyToken()</c>
/// helper was removed in Npgsql 7.0+). a shadow property keeps the Domain
/// entities persistence-ignorant. there is no DDL: <c>xmin</c> is an existing
/// system column, so the migration adds no column and EF simply appends
/// <c>WHERE xmin = @original</c> to every UPDATE/DELETE, throwing
/// <c>DbUpdateConcurrencyException</c> when the row was changed underneath it.
/// </remarks>
internal static class XminConcurrency
{
	public static void UseXminConcurrencyToken<T>(this EntityTypeBuilder<T> builder)
		where T : class
	{
		builder.Property<uint>("Version").IsRowVersion();
	}
}
