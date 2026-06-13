using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
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
		var guild = await guilds.GetByIdWithMembershipAsync(command.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.Members.All(m => m.UserId != currentUser.Id))
			return GuildFailures.NotAMember;

		var isSelf = command.TargetUserId == currentUser.Id;
		if (!isSelf)
		{
			var mask = PermissionResolver.Resolve(
				currentUser.Id, guild.OwnerId, guild.Roles, guild.MemberRoles);
			if (!PermissionResolver.HasPermission(mask, Permission.ManageNicknames))
				return GuildFailures.MissingPermission;

			if (!PermissionResolver.OutRanks(guild, currentUser.Id, command.TargetUserId))
				return GuildFailures.RoleHierarchyBlocked;
		}

		var updateResult = guild.UpdateMemberNickname(command.TargetUserId, command.Nickname);
		if (updateResult.IsFailure)
			return updateResult.Error;

		guilds.Update(guild);
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
