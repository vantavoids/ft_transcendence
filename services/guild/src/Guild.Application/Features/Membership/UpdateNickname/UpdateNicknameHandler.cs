using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Application.Features.Membership.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.UpdateNickname;

internal sealed class UpdateNicknameHandler(
	IGuildRepository guilds,
	ICurrentUser currentUser)
	: ICommandHandler<UpdateNicknameCommand, Result<MemberResponse>>
{
	public async Task<Result<MemberResponse>> HandleAsync(
		UpdateNicknameCommand command,
		CancellationToken cancellationToken = default)
	{
		// membership-gate only: editing your OWN nickname needs no permission,
		// editing someone else's needs MANAGE_NICKNAMES plus out-ranking them
		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, command.GuildId, Permission.None, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		var isSelf = command.TargetUserId == currentUser.Id;
		if (!isSelf)
		{
			if (!PermissionResolver.HasPermission(auth.Value.EffectiveMask, Permission.ManageNicknames))
				return GuildFailures.MissingPermission;

			if (!PermissionResolver.OutRanks(guild, currentUser.Id, command.TargetUserId))
				return GuildFailures.RoleHierarchyBlocked;
		}

		var updateResult = guild.UpdateMemberNickname(command.TargetUserId, command.Nickname);
		if (updateResult.IsFailure)
			return updateResult.Error;

		await guilds.SaveChangesAsync(cancellationToken);

		var member = updateResult.Value;
		var assignedRoleIds = guild.MemberRoles
			.Where(mr => mr.UserId == member.UserId)
			.Select(mr => mr.RoleId.ToString())
			.ToList();

		return new MemberResponse(
			UserId: member.UserId.ToString(),
			GuildId: guild.Id.ToString(),
			Nickname: member.Nickname,
			Roles: assignedRoleIds,
			JoinedAt: member.JoinedAt);
	}
}
