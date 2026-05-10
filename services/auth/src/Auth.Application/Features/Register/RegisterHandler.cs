using Auth.Application.Abstractions;
using Auth.Application.Abstractions.Events;
using Auth.Application.Abstractions.Messaging;
using Auth.Application.Abstractions.Persistence;
using Auth.Application.Abstractions.Security;
using Auth.Application.Events;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;

namespace Auth.Application.Features.Register;


internal sealed class RegisterHandler(
    IIdGenerator idGenerator,
    IAuthUserRepository userRepository,
    ITokenGenerator tokenGenerator,
    ISecretHasher secretHasher,
    IEventBus eventBus,
    IClock clock
) : ICommandHandler<RegisterCommand, Result<RegisterResult>>
{
    public async Task<Result<RegisterResult>> HandleAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (existingUser is not null)
            return AuthFailures.EmailAlreadyRegistered;
        
        var uid = idGenerator.NextId();
        var now = clock.UtcNow;

        var newUser = AuthUser.CreateEmailPasswordUser(
            id: uid,
            email: command.Email,
            passwordHash: secretHasher.Hash(command.Password),
            now
        );
        if (newUser.IsFailure)
            return newUser.Error;

        var accessTk = tokenGenerator.GenerateAccessToken(uid);
        var refreshTk = tokenGenerator.GenerateRefreshToken();

        newUser.Value.SetRefreshToken(secretHasher.Hash(refreshTk),
            now,
            now + tokenGenerator.GetRefreshTokenLifetime()
        );
        await userRepository.AddAsync(newUser.Value, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        var email = newUser.Value.Email!.Value;
        await eventBus.PublishAsync(new UserRegisteredEvent(uid, email), cancellationToken);

        return new RegisterResult(uid, accessTk, refreshTk);
    }
}