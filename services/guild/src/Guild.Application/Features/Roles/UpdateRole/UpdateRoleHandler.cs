using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Application.Features.Roles.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Roles.UpdateRole;

internal sealed class UpdateRoleHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	IChannelPermissionOverwriteRepository overwrites,
	IEventBus eventBus,
	IClock clock,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
	: ICommandHandler<UpdateRoleCommand, Result<RoleResponse>>
{
	public async Task<Result<RoleResponse>> HandleAsync(
		UpdateRoleCommand command,
		CancellationToken cancellationToken = default)
	{
		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, command.GuildId, Permission.ManageRoles, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;
		var mask = auth.Value.EffectiveMask;

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

		// read can only be revoked when the role loses READ_MESSAGES or ADMINISTRATOR
		// (base perms are additive, so a grant never removes read)
		const long readAffecting = (long)Permission.ReadMessages | (long)Permission.Administrator;
		ChannelReadSnapshot? snapshot = null;
		if (command.Permissions is long updatedPerms && (role.Permissions & ~updatedPerms & readAffecting) != 0)
		{
			var affected = role.IsDefault
				? guild.Members.Select(m => m.UserId).ToList()
				: guild.MemberRoles.Where(mr => mr.RoleId == command.RoleId).Select(mr => mr.UserId).Distinct().ToList();
			if (affected.Count > 0)
				snapshot = await ChannelAccess.CaptureAsync(guild, affected, channels, overwrites, cancellationToken);
		}

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

		if (snapshot is { } snap)
			await ChannelAccess.PublishRevocationsAsync(eventBus, guild, snap, cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return RoleResponse.From(updateResult.Value);
	}
}
