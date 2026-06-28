using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Contracts;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.LeaveGuild;

internal sealed class LeaveGuildHandler(
	IGuildRepository guilds,
	IEventBus eventBus,
	IClock clock,
	ICurrentUser currentUser)
	: ICommandHandler<LeaveGuildCommand, Result>
{
	public async Task<Result> HandleAsync(
		LeaveGuildCommand command,
		CancellationToken cancellationToken = default)
	{
		var guild = await guilds.GetByIdWithMembershipAsync(command.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		var removeResult = guild.RemoveMember(currentUser.Id, clock.UtcNow);
		if (removeResult.IsFailure)
			return removeResult.Error;

		// publish BEFORE SaveChanges so the bus outbox binds the GuildMemberLeft
		// event to the same transaction as the membership removal
		await eventBus.PublishAsync(
			new GuildMemberLeft(guild.Id, currentUser.Id),
			cancellationToken);

		await guilds.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
