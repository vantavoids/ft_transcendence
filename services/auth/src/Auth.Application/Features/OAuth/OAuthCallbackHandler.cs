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
            user = existingUser;
            isNewUser = false;
        }
        else
        {
            var id = idGenerator.NextId();
            var createResult = AuthUser.CreateOAuthUser(id, command.Provider, userInfo.ProviderId, now);
            if (createResult.IsFailure)
                return createResult.Error;

            user = createResult.Value;
            await repository.AddAsync(user, cancellationToken);
            isNewUser = true;
        }

        var rawRefreshToken = tokenGenerator.GenerateRefreshToken();
        var setTokenResult = user.SetRefreshToken(
            hasher.Hash(rawRefreshToken),
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
}
