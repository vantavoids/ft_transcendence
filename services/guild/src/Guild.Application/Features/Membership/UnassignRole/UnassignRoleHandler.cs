using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.UnassignRole;

internal sealed class UnassignRoleHandler(
	IGuildRepository guilds,
	IClock clock,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
	: ICommandHandler<UnassignRoleCommand, Result>
{
	public async Task<Result> HandleAsync(
		UnassignRoleCommand command,
		CancellationToken cancellationToken = default)
	{
		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, command.GuildId, Permission.ManageRoles, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		var role = guild.Roles.FirstOrDefault(r => r.Id == command.RoleId);
		if (role is null)
			return GuildFailures.RoleNotFound;

		if (!PermissionResolver.OutRanksRole(guild, currentUser.Id, role))
			return GuildFailures.RoleHierarchyBlocked;

		var unassignResult = guild.UnassignRole(command.TargetUserId, command.RoleId, clock.UtcNow);
		if (unassignResult.IsFailure)
			return unassignResult.Error;

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
