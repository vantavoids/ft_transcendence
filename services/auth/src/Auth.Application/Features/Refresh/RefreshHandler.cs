using Auth.Application.Abstractions;
using Auth.Application.Abstractions.Messaging;
using Auth.Application.Abstractions.Persistence;
using Auth.Application.Abstractions.Security;
using Auth.Domain.Results;

namespace Auth.Application.Features.Refresh;

internal sealed class RefreshHandler(
    IAuthUserRepository userRepository,
    ISecretHasher secretHasher,
    ITokenGenerator tokenGenerator,
    IClock clock
) : ICommandHandler<RefreshCommand, Result<RefreshResult>>
{
    public async Task<Result<RefreshResult>> HandleAsync(
        RefreshCommand command,
        CancellationToken cancellationToken = default)
    {
        var hash = secretHasher.HashDeterministic(command.RefreshToken);
        var user = await userRepository.GetByRefreshTokenHashAsync(hash, cancellationToken);

        if (user is null || user.IsDeleted)
            return AuthFailures.InvalidRefreshToken;

        if (user.RefreshToken is null || !user.RefreshToken.IsActive(clock.UtcNow))
            return AuthFailures.InvalidRefreshToken;

        var newAccessToken = tokenGenerator.GenerateAccessToken(user.Id);
        var newRefreshToken = tokenGenerator.GenerateRefreshToken();
        var now = clock.UtcNow;

        user.SetRefreshToken(
            secretHasher.HashDeterministic(newRefreshToken),
            now,
            now.Add(tokenGenerator.GetRefreshTokenLifetime())
        );

        await userRepository.SaveChangesAsync(cancellationToken);

        return new RefreshResult(newAccessToken, newRefreshToken);
    }
}
