using Guild.Application.Abstractions.Persistence;
using Guild.Persistence.Db;

namespace Guild.Persistence.Repositories;

/// <summary>
/// commits the scoped <see cref="GuildDbContext"/> - the single EF unit of work
/// that every repository tracks changes against.
/// </summary>
internal sealed class UnitOfWork(GuildDbContext context) : IUnitOfWork
{
	public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
		context.SaveChangesAsync(cancellationToken);
}
