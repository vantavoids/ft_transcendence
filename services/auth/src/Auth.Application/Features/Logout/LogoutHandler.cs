using Auth.Application.Abstractions;
using Auth.Application.Abstractions.Events;
using Auth.Application.Abstractions.Messaging;
using Auth.Application.Abstractions.Persistence;
using Auth.Application.Events;
using Auth.Domain.Results;

namespace Auth.Application.Features.Logout;

internal sealed class LogoutHandler(
    IAuthUserRepository userRepository,
    IEventBus eventBus,
    IClock clock
) : ICommandHandler<LogoutCommand, Result>
{
    public async Task<Result> HandleAsync(
        LogoutCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || user.IsDeleted)
            return AuthFailures.InvalidAccessToken;

        user.RevokeRefreshToken(clock.UtcNow);
        await userRepository.SaveChangesAsync(cancellationToken);

        await eventBus.PublishAsync(new UserLoggedOutEvent(command.UserId), cancellationToken);

        return Result.Ok();
    }
}
