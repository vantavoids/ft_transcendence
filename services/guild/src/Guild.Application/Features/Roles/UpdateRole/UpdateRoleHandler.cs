using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Features.Roles.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Roles.UpdateRole;

internal sealed class UpdateRoleHandler(
	IGuildRepository guilds,
	IClock clock,
	ICurrentUser currentUser)
	: ICommandHandler<UpdateRoleCommand, Result<RoleResponse>>
{
	public async Task<Result<RoleResponse>> HandleAsync(
		UpdateRoleCommand command,
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

		// hierarchy guard: caller must out-rank the role's position. owner
		// short-circuits via Rank=int.MaxValue. position itself is no longer
		// editable here; reordering goes through PATCH /guilds/{id}/roles
		if (!PermissionResolver.OutRanksRole(guild, currentUser.Id, role))
			return GuildFailures.RoleHierarchyBlocked;

		// permission grants follow the same "can't grant what you lack" rule as
		// POST; if Permissions is unchanged (null) the check is skipped
		if (command.Permissions is long newPerms
		    && !PermissionResolver.CanGrantPermissions(mask, newPerms))
			return GuildFailures.CannotGrantPermissionsYouLack;

		var updateResult = guild.UpdateRole(
			roleId: command.RoleId,
			name: command.Name,
			color: command.Color,
			permissions: command.Permissions,
			isHoisted: command.IsHoisted,
			isMentionable: command.IsMentionable,
			now: clock.UtcNow);
		if (updateResult.IsFailure)
			return updateResult.Error;

		guilds.Update(guild);
		await guilds.SaveChangesAsync(cancellationToken);

		return RoleResponse.From(updateResult.Value);
	}
}
