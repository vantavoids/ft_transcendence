using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Features.Roles.Common;
using Guild.Application.Features.Roles.ListRoles;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Roles.ReorderRoles;

internal sealed class ReorderRolesHandler(
	IGuildRepository guilds,
	IClock clock,
	ICurrentUser currentUser)
	: ICommandHandler<ReorderRolesCommand, Result<RoleListResponse>>
{
	public async Task<Result<RoleListResponse>> HandleAsync(
		ReorderRolesCommand command,
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

		// hierarchy guard (Discord rule): for every role whose position actually
		// CHANGES, the caller must strictly out-rank it at both its old and new
		// position, so a MANAGE_ROLES holder can neither reorder anything at/above
		// themselves nor push anything to at/above their own rank. roles left at
		// their current position carry no hierarchy implication and are skipped --
		// this matters because the full-permutation body always includes the
		// caller's own highest role (old == new, Rank == its position), which must
		// not trip the guard. owner short-circuits via Rank=int.MaxValue. structural
		// problems (unknown/duplicate/default ids) are left to the domain; we skip
		// them here so the guard never throws and never leaks structure to a caller
		// who would fail validation regardless
		var rolesById = guild.Roles.ToDictionary(r => r.Id);
		var callerRank = PermissionResolver.Rank(guild, currentUser.Id);
		foreach (var move in command.Moves)
		{
			if (!rolesById.TryGetValue(move.RoleId, out var role) || role.IsDefault)
				continue;
			if (move.Position == role.Position)
				continue;
			if (callerRank <= role.Position || callerRank <= move.Position)
				return GuildFailures.RoleHierarchyBlocked;
		}

		var moves = command.Moves.Select(m => (m.RoleId, m.Position)).ToList();
		var result = guild.ReorderRoles(moves, clock.UtcNow);
		if (result.IsFailure)
			return result.Error;

		guilds.Update(guild);
		await guilds.SaveChangesAsync(cancellationToken);

		var items = result.Value.Select(RoleResponse.From).ToList();
		return new RoleListResponse(items);
	}
}
