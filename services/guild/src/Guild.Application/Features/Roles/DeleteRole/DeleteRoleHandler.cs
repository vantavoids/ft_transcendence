using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Application.Contracts;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Roles.DeleteRole;

internal sealed class DeleteRoleHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	IChannelPermissionOverwriteRepository overwrites,
	IEventBus eventBus,
	IClock clock,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
	: ICommandHandler<DeleteRoleCommand, Result>
{
	public async Task<Result> HandleAsync(
		DeleteRoleCommand command,
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

		// CannotDeleteDefaultRole short-circuits the hierarchy check so the
		// response reads as a clear validation error rather than a permissions
		// problem (members can have @everyone at position 0 and "out-rank" it)
		if (role.IsDefault)
			return GuildFailures.CannotDeleteDefaultRole;

		if (!PermissionResolver.OutRanksRole(guild, currentUser.Id, role))
			return GuildFailures.RoleHierarchyBlocked;

		// no holders -> deleting the role can't change any member's read
		var holders = guild.MemberRoles
			.Where(mr => mr.RoleId == command.RoleId)
			.Select(mr => mr.UserId)
			.Distinct()
			.ToList();
		ChannelReadSnapshot? snapshot = holders.Count > 0
			? await ChannelAccess.CaptureAsync(guild, holders, channels, overwrites, cancellationToken)
			: null;

		var removeResult = guild.RemoveRole(command.RoleId, clock.UtcNow);
		if (removeResult.IsFailure)
			return removeResult.Error;

		if (snapshot is { } snap)
			await ChannelAccess.PublishRevocationsAsync(eventBus, guild, snap, cancellationToken);

		await eventBus.PublishAsync(new GuildRolesChanged(command.GuildId), cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
