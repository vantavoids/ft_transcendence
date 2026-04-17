using Auth.Application.Abstractions.Persistence;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Auth.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace Auth.Persistence.Repositories;

internal sealed class AuthUserRepository(AuthDbContext context) : IAuthUserRepository
{
    public async Task<AuthUser?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.AuthUsers
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<AuthUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await context.AuthUsers
            .FirstOrDefaultAsync(u => u.Email != null && u.Email!.Value == email, cancellationToken);
    }

    public async Task<AuthUser?> GetByOAuthAsync(
        OAuthProvider provider,
        string oauthId,
        CancellationToken cancellationToken = default)
    {
        return await context.AuthUsers
            .FirstOrDefaultAsync(
                u => u.OAuthIdentity != null
                    && u.OAuthIdentity.Provider == provider
                    && u.OAuthIdentity.Id == oauthId,
                cancellationToken);
    }

    public async Task AddAsync(AuthUser user, CancellationToken cancellationToken = default)
    {
        await context.AuthUsers.AddAsync(user, cancellationToken);
    }

    public void Update(AuthUser user)
    {
        context.AuthUsers.Update(user);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
