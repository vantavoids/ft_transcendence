using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Application.Contracts;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.KickMember;

internal sealed class KickMemberHandler(
	IGuildRepository guilds,
	IEventBus eventBus,
	IClock clock,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
	: ICommandHandler<KickMemberCommand, Result>
{
	public async Task<Result> HandleAsync(
		KickMemberCommand command,
		CancellationToken cancellationToken = default)
	{
		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, command.GuildId, Permission.KickMembers, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		// kick rejects a non-member target up front: you cannot remove someone who
		// is not in the guild. (BanMember deliberately does NOT do this, because a
		// pre-emptive ban against a never-joined user is legal - see that handler.)
		if (guild.Members.All(m => m.UserId != command.TargetUserId))
			return GuildFailures.TargetNotAMember;

		if (command.TargetUserId == guild.OwnerId)
			return GuildFailures.CannotKickOwner;

		if (!PermissionResolver.OutRanks(guild, currentUser.Id, command.TargetUserId))
			return GuildFailures.RoleHierarchyBlocked;

		var removeResult = guild.RemoveMember(command.TargetUserId, clock.UtcNow);
		if (removeResult.IsFailure)
			return removeResult.Error;

		// publish BEFORE SaveChanges: the bus outbox records the event as an
		// OutboxMessage row in this same transaction, so the kick and the
		// GuildMemberLeft event commit atomically (or not at all)
		await eventBus.PublishAsync(
			new GuildMemberLeft(guild.Id, command.TargetUserId),
			cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
