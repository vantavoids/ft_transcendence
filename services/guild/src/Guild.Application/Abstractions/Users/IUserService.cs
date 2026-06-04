namespace Guild.Application.Abstractions.Users;

/// <summary>
/// thin client around the User Service docker-network endpoints (see
/// <c>docs/contracts/user.md</c>). Implementations gracefully degrade on
/// connect failure: callers receive <c>false</c> from <see cref="ExistsAsync"/>
/// and <c>null</c> from <see cref="GetSummaryAsync"/>, never an exception.
/// the join / invite flows treat existence as best-effort defence in depth
/// (the JWT already proves the caller authenticated at Auth Service) so a
/// User Service outage must not break joining.
/// </summary>
public interface IUserService
{
	Task<bool> ExistsAsync(long userId, CancellationToken cancellationToken = default);

	Task<UserSummary?> GetSummaryAsync(long userId, CancellationToken cancellationToken = default);
}

public sealed record UserSummary(long Id, string Username);
