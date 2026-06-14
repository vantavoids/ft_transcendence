using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Features.Roles.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Roles.CreateRole;

internal sealed class CreateRoleHandler(
	IGuildRepository guilds,
	IIdGenerator ids,
	IClock clock,
	ICurrentUser currentUser)
	: ICommandHandler<CreateRoleCommand, Result<RoleResponse>>
{
	public async Task<Result<RoleResponse>> HandleAsync(
		CreateRoleCommand command,
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

		guilds.Update(guild);
		await guilds.SaveChangesAsync(cancellationToken);

		return RoleResponse.From(addResult.Value);
	}
}
