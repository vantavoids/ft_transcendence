using Auth.Domain.AuthUser;

namespace Auth.Application.Abstractions.Persistence;

public interface IAuthUserRepository
{
    Task<AuthUser?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<AuthUser?> GetByRefreshTokenHashAsync(
        string hash, CancellationToken cancellationToken = default);

    Task<AuthUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AuthUser?> GetByOAuthAsync(
        OAuthProvider provider,
        string oauthId,
        CancellationToken cancellationToken = default);

    Task AddAsync(AuthUser user, CancellationToken cancellationToken = default);
    void Update(AuthUser user);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}