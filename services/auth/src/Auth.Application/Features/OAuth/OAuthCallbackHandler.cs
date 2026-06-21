using Auth.Application.Abstractions;
using Auth.Application.Abstractions.Events;
using Auth.Application.Abstractions.Messaging;
using Auth.Application.Abstractions.OAuth;
using Auth.Application.Abstractions.Persistence;
using Auth.Application.Abstractions.Security;
using Auth.Application.Events;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Application.Features.OAuth;

internal sealed class OAuthCallbackHandler(
    IServiceProvider sp,
    IAuthUserRepository repository,
    ITokenGenerator tokenGenerator,
    ISecretHasher hasher,
    IIdGenerator idGenerator,
    IClock clock,
    IEventBus eventBus)
    : ICommandHandler<OAuthCallbackCommand, Result<OAuthCallbackResult>>
{
    public async Task<Result<OAuthCallbackResult>> HandleAsync(
        OAuthCallbackCommand command, CancellationToken cancellationToken = default)
    {
        var client = sp.GetKeyedService<IOAuthProviderClient>(command.Provider);
        if (client is null)
            return AuthFailures.InvalidOAuthProvider;

        var userInfoResult = await client.ExchangeCodeAsync(command.Code, command.State, cancellationToken);
        if (userInfoResult.IsFailure)
            return userInfoResult.Error;

        var userInfo = userInfoResult.Value;
        var now = clock.UtcNow;

        var existingUser = await repository.GetByOAuthAsync(command.Provider, userInfo.ProviderId, cancellationToken);

        bool isNewUser;
        AuthUser user;

        if (existingUser is not null)
        {
            UpdateOAuthUser(now, existingUser, userInfo.Email, userInfo.EmailVerified == true);
            user = existingUser;
            isNewUser = false;
        }
        else
        {
            var newUserResult = await CreateOAuthUser(now, command.Provider, userInfo, cancellationToken);
            if (newUserResult.IsFailure)
                return newUserResult.Error;

            user = newUserResult.Value;
            isNewUser = true;
        }

        var rawRefreshToken = tokenGenerator.GenerateRefreshToken();
        var setTokenResult = user.SetRefreshToken(
            hasher.HashDeterministic(rawRefreshToken),
            now,
            now.Add(tokenGenerator.GetRefreshTokenLifetime()));
        if (setTokenResult.IsFailure)
            return setTokenResult.Error;

        await repository.SaveChangesAsync(cancellationToken);
        if (isNewUser)
            await eventBus.PublishAsync(
                new UserRegisteredEvent(user.Id, userInfo.Email ?? string.Empty), cancellationToken);

        var accessToken = tokenGenerator.GenerateAccessToken(user.Id);
        return new OAuthCallbackResult(accessToken, rawRefreshToken, isNewUser);
    }

    private static void UpdateOAuthUser(DateTimeOffset now, AuthUser existing, string ?email, bool verified)
    {
        if (!string.IsNullOrWhiteSpace(email) && (existing.Email is null || existing.Email.Value != email))
            existing.SetEmail(now, email, verified);

        if (existing.Email is not null && !existing.Email.IsVerified && verified)
            existing.VerifyEmail(now);
    }

    private async Task<Result<AuthUser>> CreateOAuthUser(DateTimeOffset now, OAuthProvider provider, OAuthUserInfo userInfo, CancellationToken ct)
    {
        var id = idGenerator.NextId();
        var createResult = AuthUser.CreateOAuthUser(id, provider, userInfo.ProviderId, now);
        if (createResult.IsFailure)
            return createResult.Error;

        var user = createResult.Value;
        await repository.AddAsync(user, ct);
        UpdateOAuthUser(now, user, userInfo.Email, userInfo.EmailVerified == true);
        return user;
    }
}
