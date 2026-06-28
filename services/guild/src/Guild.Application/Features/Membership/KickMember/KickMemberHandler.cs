using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Contracts;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.KickMember;

internal sealed class KickMemberHandler(
	IGuildRepository guilds,
	IEventBus eventBus,
	IClock clock,
	ICurrentUser currentUser)
	: ICommandHandler<KickMemberCommand, Result>
{
	public async Task<Result> HandleAsync(
		KickMemberCommand command,
		CancellationToken cancellationToken = default)
	{
		var guild = await guilds.GetByIdWithMembershipAsync(command.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.Members.All(m => m.UserId != currentUser.Id))
			return GuildFailures.NotAMember;

		if (guild.Members.All(m => m.UserId != command.TargetUserId))
			return GuildFailures.TargetNotAMember;

		var mask = PermissionResolver.Resolve(
			currentUser.Id, guild.OwnerId, guild.Roles, guild.MemberRoles);
		if (!PermissionResolver.HasPermission(mask, Permission.KickMembers))
			return GuildFailures.MissingPermission;

		if (command.TargetUserId == guild.OwnerId)
			return GuildFailures.CannotKickOwner;

		if (!PermissionResolver.OutRanks(guild, currentUser.Id, command.TargetUserId))
			return GuildFailures.RoleHierarchyBlocked;

		var removeResult = guild.RemoveMember(command.TargetUserId, clock.UtcNow);
		if (removeResult.IsFailure)
			return removeResult.Error;

		guilds.Update(guild);

		// publish BEFORE SaveChanges: the bus outbox records the event as an
		// OutboxMessage row in this same transaction, so the kick and the
		// GuildMemberLeft event commit atomically (or not at all)
		await eventBus.PublishAsync(
			new GuildMemberLeft(guild.Id, command.TargetUserId),
			cancellationToken);

		await guilds.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
