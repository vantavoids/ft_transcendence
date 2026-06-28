using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.GetMemberPermissions;

internal sealed class GetMemberPermissionsHandler(
	IGuildRepository guilds,
	ICurrentUser currentUser)
	: IQueryHandler<GetMemberPermissionsQuery, Result<MemberPermissionsResponse>>
{
	public async Task<Result<MemberPermissionsResponse>> HandleAsync(
		GetMemberPermissionsQuery query,
		CancellationToken cancellationToken = default)
	{
		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, query.GuildId, Permission.None, cancellationToken, asNoTracking: true);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		if (guild.Members.All(m => m.UserId != query.TargetUserId))
			return GuildFailures.TargetNotAMember;

		var isOwner = query.TargetUserId == guild.OwnerId;
		var effective = PermissionResolver.Resolve(guild, query.TargetUserId);

		// @everyone is implicit for every member; explicit assignments add to that.
		// the owner additionally sees the seeded Administrator role.
		var assignedRoleIds = guild.MemberRoles
			.Where(mr => mr.UserId == query.TargetUserId)
			.Select(mr => mr.RoleId)
			.ToHashSet();

		var roles = guild.Roles
			.Where(r => r.IsDefault || assignedRoleIds.Contains(r.Id))
			.OrderBy(r => r.Position)
			.Select(r => new MemberRoleSnapshot(r.Id.ToString(), r.Name, r.Permissions))
			.ToList();

		return new MemberPermissionsResponse(
			UserId: query.TargetUserId.ToString(),
			GuildId: guild.Id.ToString(),
			Roles: roles,
			EffectivePermissions: effective,
			IsOwner: isOwner);
	}
}
