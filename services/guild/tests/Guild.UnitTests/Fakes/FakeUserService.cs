using Guild.Application.Abstractions.Users;

namespace Guild.UnitTests.Fakes;

internal sealed class FakeUserService : IUserService
{
	private readonly Dictionary<long, UserSummary> _summaries = new();

	public bool Exists { get; set; } = true;
	public int ExistsCallCount { get; private set; }
	public int GetSummaryCallCount { get; private set; }

	public void RegisterSummary(long userId, string username)
		=> _summaries[userId] = new UserSummary(userId, username);

	public Task<bool> ExistsAsync(long userId, CancellationToken cancellationToken = default)
	{
		ExistsCallCount++;
		return Task.FromResult(Exists);
	}

	public Task<UserSummary?> GetSummaryAsync(long userId, CancellationToken cancellationToken = default)
	{
		GetSummaryCallCount++;
		_summaries.TryGetValue(userId, out var summary);
		return Task.FromResult<UserSummary?>(summary);
	}
}
