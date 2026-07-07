using Auth.Application.Abstractions;
using Auth.Application.Abstractions.Events;
using Auth.Application.Abstractions.Messaging;
using Auth.Application.Abstractions.Persistence;
using Auth.Application.Contracts;
using Auth.Domain.Results;

namespace Auth.Application.Features.DeleteMe;

internal sealed class DeleteMeHandler(
    IAuthUserRepository userRepository,
    IGuildClient guildClient,
    IEventBus eventBus,
    IClock clock
) : ICommandHandler<DeleteMeCommand, Result>
{
    public async Task<Result> HandleAsync(
        DeleteMeCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || user.IsDeleted)
            return AuthFailures.InvalidAccessToken;

        var ownedCount = await guildClient.GetOwnedGuildsCountAsync(command.UserId, cancellationToken);
        if (ownedCount > 0)
            return AuthFailures.OwnedGuildsConflict;

        user.SoftDelete(clock.UtcNow);
        user.RevokeRefreshToken(clock.UtcNow);

        await userRepository.SaveChangesAsync(cancellationToken);
        await eventBus.PublishAsync(new UserDeleted(command.UserId, user.Email?.Value), cancellationToken);

        return Result.Ok();
    }
}
