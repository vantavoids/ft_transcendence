using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Application.Contracts;
using Guild.Application.Features.Roles.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Roles.CreateRole;

internal sealed class CreateRoleHandler(
	IGuildRepository guilds,
	IEventBus eventBus,
	IIdGenerator ids,
	IClock clock,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
	: ICommandHandler<CreateRoleCommand, Result<RoleResponse>>
{
	public async Task<Result<RoleResponse>> HandleAsync(
		CreateRoleCommand command,
		CancellationToken cancellationToken = default)
	{
		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, command.GuildId, Permission.ManageRoles, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;
		var mask = auth.Value.EffectiveMask;

		if (!PermissionResolver.CanGrantPermissions(mask, command.Permissions))
			return GuildFailures.CannotGrantPermissionsYouLack;

		var addResult = guild.AddRole(
			roleId: ids.NextId(),
			name: command.Name,
			color: command.Color,
			permissions: command.Permissions,
			isHoisted: command.IsHoisted,
			isMentionable: command.IsMentionable,
			now: clock.UtcNow);
		if (addResult.IsFailure)
			return addResult.Error;

		await eventBus.PublishAsync(new GuildRolesChanged(command.GuildId), cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return RoleResponse.From(addResult.Value);
	}
}
