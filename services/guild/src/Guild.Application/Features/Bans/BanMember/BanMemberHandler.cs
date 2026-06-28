using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Application.Contracts;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Bans.BanMember;

internal sealed class BanMemberHandler(
	IGuildRepository guilds,
	IGuildBanRepository bans,
	IEventBus eventBus,
	IClock clock,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
	: ICommandHandler<BanMemberCommand, Result>
{
	public async Task<Result> HandleAsync(
		BanMemberCommand command,
		CancellationToken cancellationToken = default)
	{
		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, command.GuildId, Permission.BanMembers, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		// NOTE: unlike KickMember, there is intentionally no TargetNotAMember guard
		// here - banning a user who has never joined is a legal pre-emptive ban.
		// the `wasMember` branch below only removes membership / emits the left
		// event when the target actually was in the guild.
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

		bans.Add(banResult.Value);

		if (wasMember)
		{
			var removeResult = guild.RemoveMember(command.TargetUserId, now);
			if (removeResult.IsFailure)
				return removeResult.Error;

			// publish BEFORE SaveChanges so the bus outbox binds the event to the
			// same transaction as the ban insert + member removal. only a member
			// who was actually in the guild produces a GuildMemberLeft (a
			// pre-emptive ban against a non-member has nothing to announce)
			await eventBus.PublishAsync(
				new GuildMemberLeft(guild.Id, command.TargetUserId),
				cancellationToken);
		}

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
