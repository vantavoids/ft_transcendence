using Auth.Application.Abstractions;
using Auth.Application.Abstractions.Messaging;
using Auth.Application.Abstractions.Persistence;
using Auth.Application.Abstractions.Security;
using Auth.Domain.Results;

namespace Auth.Application.Features.Login;

internal sealed class LoginCommandHandler(
    IAuthUserRepository userRepository,
    ISecretHasher secretHasher,
    ITokenGenerator tokenGenerator,
    IClock clock
) : ICommandHandler<LoginCommand, Result<LoginResult>>
{
    public async Task<Result<LoginResult>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null || user.IsDeleted || user.PasswordHash is null)
            return AuthFailures.InvalidCredentials;

        if (!secretHasher.Verify(command.Password, user.PasswordHash))
            return AuthFailures.InvalidCredentials;

        var accessToken = tokenGenerator.GenerateAccessToken(user.Id);
        var refreshToken = tokenGenerator.GenerateRefreshToken();

        var refreshTokenHash = secretHasher.Hash(refreshToken);
        var now = clock.UtcNow;
        user.SetRefreshToken(
            refreshTokenHash,
            now,
            now.Add(tokenGenerator.GetRefreshTokenLifetime())
        );
        await userRepository.SaveChangesAsync(cancellationToken);

        return new LoginResult(
            UserId: user.Id,
            AccessToken: accessToken,
            RefreshToken: refreshToken
        );
    }
}
