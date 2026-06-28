using Auth.Application.Abstractions;

namespace Auth.UnitTests.Fakes;

internal sealed class FakeGuildClient : IGuildClient
{
    public int OwnedGuildsCount { get; set; } = 0;

    public Task<int> GetOwnedGuildsCountAsync(long userId, CancellationToken cancellationToken = default)
        => Task.FromResult(OwnedGuildsCount);
}
