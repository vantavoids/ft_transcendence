namespace Auth.Application.Abstractions;

public interface IGuildClient
{
    Task<int> GetOwnedGuildsCountAsync(long userId, CancellationToken cancellationToken = default);
}
