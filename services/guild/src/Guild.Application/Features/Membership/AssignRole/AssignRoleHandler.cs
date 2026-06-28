using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.AssignRole;

internal sealed class AssignRoleHandler(
	IGuildRepository guilds,
	IClock clock,
	ICurrentUser currentUser)
	: ICommandHandler<AssignRoleCommand, Result>
{
	public async Task<Result> HandleAsync(
		AssignRoleCommand command,
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

		if (!PermissionResolver.OutRanksRole(guild, currentUser.Id, role))
			return GuildFailures.RoleHierarchyBlocked;

		if (!PermissionResolver.CanGrantPermissions(mask, role.Permissions))
			return GuildFailures.CannotGrantPermissionsYouLack;

		var assignResult = guild.AssignRole(command.TargetUserId, command.RoleId, clock.UtcNow);
		if (assignResult.IsFailure)
			return assignResult.Error;

		await guilds.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
