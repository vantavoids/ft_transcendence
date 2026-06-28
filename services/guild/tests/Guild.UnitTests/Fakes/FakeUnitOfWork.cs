using Guild.Application.Abstractions.Persistence;

namespace Guild.UnitTests.Fakes;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
	public int SaveChangesCount { get; private set; }

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		SaveChangesCount++;
		return Task.CompletedTask;
	}
}
