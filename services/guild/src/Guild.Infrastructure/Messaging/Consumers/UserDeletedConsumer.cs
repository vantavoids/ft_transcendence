using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Users.PurgeUserData;
using Guild.Infrastructure.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Guild.Infrastructure.Messaging.Consumers;

/// <summary>
/// consumes Auth's <c>user.deleted</c> and dispatches the Guild-side GDPR erasure
/// cascade. thin adapter over <see cref="PurgeUserDataCommand"/>; the handler owns
/// the policy and its steps are idempotent, so redelivery is safe.
/// </summary>
public sealed class UserDeletedConsumer(
	ICommandHandler<PurgeUserDataCommand> handler,
	ILogger<UserDeletedConsumer> logger)
	: IConsumer<UserDeleted>
{
	public async Task Consume(ConsumeContext<UserDeleted> context)
	{
		var userId = context.Message.UserId;

		await handler.HandleAsync(new PurgeUserDataCommand(userId), context.CancellationToken);

		logger.LogInformation("user.deleted consumed: purged Guild data for user_id={UserId}", userId);
	}
}
