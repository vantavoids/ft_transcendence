using Auth.Application.Abstractions.Persistence;
using Auth.Domain.AuthUser;

namespace Auth.UnitTests.Fakes;

internal sealed class FakeAuthUserRepository : IAuthUserRepository
{
    private readonly Dictionary<long, AuthUser> _store = new();

    public IReadOnlyDictionary<long, AuthUser> Store => _store;

    public Task<AuthUser?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<AuthUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = _store.Values.FirstOrDefault(u =>
            u.Email?.Value.Equals(email, StringComparison.OrdinalIgnoreCase) == true);
        return Task.FromResult(user);
    }

    public Task<AuthUser?> GetByOAuthAsync(
        OAuthProvider provider, string oauthId, CancellationToken cancellationToken = default)
    {
        var user = _store.Values.FirstOrDefault(u =>
            u.OAuthIdentity?.Provider == provider &&
            u.OAuthIdentity.Id == oauthId);
        return Task.FromResult(user);
    }

    public Task AddAsync(AuthUser user, CancellationToken cancellationToken = default)
    {
        _store[user.Id] = user;
        return Task.CompletedTask;
    }

    public void Update(AuthUser user) => _store[user.Id] = user;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
