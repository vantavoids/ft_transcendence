namespace Guild.Application.Abstractions.Persistence;

/// <summary>
/// the transaction boundary. all repositories share one scoped DbContext, so
/// committing is a cross-aggregate concern that does not belong on any single
/// repository (a ban handler used to call <c>guilds.SaveChangesAsync()</c> just
/// to commit). handlers stage changes through their repositories, then call
/// <see cref="SaveChangesAsync"/> once to persist them all atomically.
/// </summary>
public interface IUnitOfWork
{
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
