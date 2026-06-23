using Auth.Application.Abstractions;
using Auth.Application.Abstractions.Messaging;
using Auth.Application.Abstractions.Persistence;
using Auth.Application.Abstractions.Security;
using Auth.Application.Features.GetMe;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;

namespace Auth.Application.Features.UpdateMe;

internal sealed class UpdateMeHandler(
    IAuthUserRepository userRepository,
    ISecretHasher secretHasher,
    IClock clock
) : ICommandHandler<UpdateMeCommand, Result<GetMeResponse>>
{
    public async Task<Result<GetMeResponse>> HandleAsync(
        UpdateMeCommand command,
        CancellationToken cancellationToken = default)
    {
        var userResult = await ValidateAndFetchAsync(command, cancellationToken);
        if (userResult.IsFailure)
            return userResult.Error;

        var user = userResult.Value;

        string? newPasswordHash = null;
        if (command.NewPassword is not null)
            newPasswordHash = secretHasher.Hash(command.NewPassword);

        var updateResult = user.Update(
            clock.UtcNow,
            email: command.Email,
            passwordHash: newPasswordHash,
            emailVerified: command.Email is not null ? false : null);

        if (updateResult.IsFailure)
            return updateResult.Error;

        if (newPasswordHash is not null)
            user.RevokeRefreshToken(clock.UtcNow);

        await userRepository.SaveChangesAsync(cancellationToken);

        var oauthProviders = user.OAuthIdentity is not null
            ? [user.OAuthIdentity.Provider.ToSlug()]
            : Array.Empty<string>();

        return new GetMeResponse(
            Id: user.Id.ToString(),
            Email: user.Email?.Value,
            EmailVerified: user.Email?.IsVerified ?? false,
            OAuthProviders: oauthProviders,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt);
    }

    private async Task<Result<AuthUser>> ValidateAndFetchAsync(
        UpdateMeCommand command,
        CancellationToken cancellationToken)
    {
        if (command is { CurrentPassword: null, NewPassword: null, Email: null })
            return AuthFailures.AtLeastOneFieldToPatch;

        if (command.NewPassword is not null && string.IsNullOrWhiteSpace(command.NewPassword))
            return AuthFailures.WeakPassword;

        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || user.IsDeleted)
            return AuthFailures.InvalidAccessToken;

        if (user.HasOAuthCredentials)
            return AuthFailures.OAuthCantPatchEmail;

        if (command.CurrentPassword is null ||
            !secretHasher.Verify(command.CurrentPassword, user.PasswordHash!))
            return AuthFailures.InvalidCredentials;

        if (command.Email is null)
            return user;

        var existing = await userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (existing is not null && existing.Id != user.Id)
            return AuthFailures.EmailAlreadyRegistered;

        return user;
    }
}
