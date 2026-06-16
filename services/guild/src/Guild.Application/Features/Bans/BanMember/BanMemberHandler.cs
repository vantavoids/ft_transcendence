using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Contracts;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Bans.BanMember;

internal sealed class BanMemberHandler(
	IGuildRepository guilds,
	IGuildBanRepository bans,
	IEventBus eventBus,
	IClock clock,
	ICurrentUser currentUser)
	: ICommandHandler<BanMemberCommand, Result>
{
	public async Task<Result> HandleAsync(
		BanMemberCommand command,
		CancellationToken cancellationToken = default)
	{
		var guild = await guilds.GetByIdWithMembershipAsync(command.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.Members.All(m => m.UserId != currentUser.Id))
			return GuildFailures.NotAMember;

		var mask = PermissionResolver.Resolve(
			currentUser.Id, guild.OwnerId, guild.Roles, guild.MemberRoles);
		if (!PermissionResolver.HasPermission(mask, Permission.BanMembers))
			return GuildFailures.MissingPermission;

		if (command.TargetUserId == currentUser.Id)
			return GuildFailures.CannotBanSelf;

		if (command.TargetUserId == guild.OwnerId)
			return GuildFailures.CannotBanOwner;

		// hierarchy is only meaningful when the target is a member; pre-emptive
		// bans against non-members carry no role so there is nothing to outrank
		var wasMember = guild.Members.Any(m => m.UserId == command.TargetUserId);
		if (wasMember && !PermissionResolver.OutRanks(guild, currentUser.Id, command.TargetUserId))
			return GuildFailures.RoleHierarchyBlocked;

		var existing = await bans.FindAsync(guild.Id, command.TargetUserId, cancellationToken);
		if (existing is not null)
			return GuildFailures.AlreadyBanned;

		var now = clock.UtcNow;
		var banResult = GuildBan.Create(
			guildId: guild.Id,
			userId: command.TargetUserId,
			bannedBy: currentUser.Id,
			reason: command.Reason,
			now: now);
		if (banResult.IsFailure)
			return banResult.Error;

		await bans.AddAsync(banResult.Value, cancellationToken);

		if (wasMember)
		{
			var removeResult = guild.RemoveMember(command.TargetUserId, now);
			if (removeResult.IsFailure)
				return removeResult.Error;
			guilds.Update(guild);
		}

		await guilds.SaveChangesAsync(cancellationToken);

		if (wasMember)
		{
			await eventBus.PublishAsync(
				new GuildMemberLeft(guild.Id, command.TargetUserId),
				cancellationToken);
		}

		return Result.Ok();
	}
}
