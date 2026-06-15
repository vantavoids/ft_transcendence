using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.UnassignRole;

internal sealed class UnassignRoleHandler(
	IGuildRepository guilds,
	IClock clock,
	ICurrentUser currentUser)
	: ICommandHandler<UnassignRoleCommand, Result>
{
	public async Task<Result> HandleAsync(
		UnassignRoleCommand command,
		CancellationToken cancellationToken = default)
	{
		var guild = await guilds.GetByIdWithMembershipAsync(command.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.Members.All(m => m.UserId != currentUser.Id))
			return GuildFailures.NotAMember;

		var mask = PermissionResolver.Resolve(
			currentUser.Id, guild.OwnerId, guild.Roles, guild.MemberRoles);
		if (!PermissionResolver.HasPermission(mask, Permission.ManageRoles))
			return GuildFailures.MissingPermission;

		var role = guild.Roles.FirstOrDefault(r => r.Id == command.RoleId);
		if (role is null)
			return GuildFailures.RoleNotFound;

		if (!PermissionResolver.OutRanksRole(guild, currentUser.Id, role))
			return GuildFailures.RoleHierarchyBlocked;

		var unassignResult = guild.UnassignRole(command.TargetUserId, command.RoleId, clock.UtcNow);
		if (unassignResult.IsFailure)
			return unassignResult.Error;

		guilds.Update(guild);
		await guilds.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
