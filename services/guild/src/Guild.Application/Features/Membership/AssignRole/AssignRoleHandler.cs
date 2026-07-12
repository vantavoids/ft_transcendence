using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Application.Contracts;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.AssignRole;

internal sealed class AssignRoleHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	IChannelPermissionOverwriteRepository overwrites,
	IEventBus eventBus,
	IClock clock,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
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

		// assigning a role adds its deny-overwrites to the member, which can revoke
		// read on channels the role denies (base perms only ever add)
		var snapshot = await ChannelAccess.CaptureAsync(
			guild, [command.TargetUserId], channels, overwrites, cancellationToken);

		var assignResult = guild.AssignRole(command.TargetUserId, command.RoleId, clock.UtcNow);
		if (assignResult.IsFailure)
			return assignResult.Error;

		await ChannelAccess.PublishRevocationsAsync(eventBus, guild, snapshot, cancellationToken);

		await eventBus.PublishAsync(
			new GuildMemberUpdated(command.GuildId, command.TargetUserId), cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
