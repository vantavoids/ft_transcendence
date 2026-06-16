using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Bans.UnbanMember;

internal sealed class UnbanMemberHandler(
	IGuildRepository guilds,
	IGuildBanRepository bans,
	ICurrentUser currentUser)
	: ICommandHandler<UnbanMemberCommand, Result>
{
	public async Task<Result> HandleAsync(
		UnbanMemberCommand command,
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

		var ban = await bans.FindAsync(guild.Id, command.TargetUserId, cancellationToken);
		if (ban is null)
			return GuildFailures.BanNotFound;

		bans.Remove(ban);
		await guilds.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
