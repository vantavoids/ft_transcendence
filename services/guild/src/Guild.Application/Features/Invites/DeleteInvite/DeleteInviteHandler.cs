using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Invites.DeleteInvite;

internal sealed class DeleteInviteHandler(
	IGuildRepository guilds,
	IGuildInviteRepository invites,
	ICurrentUser currentUser)
	: ICommandHandler<DeleteInviteCommand, Result>
{
	public async Task<Result> HandleAsync(
		DeleteInviteCommand command,
		CancellationToken cancellationToken = default)
	{
		var invite = await invites.GetByCodeAsync(command.Code, cancellationToken);
		if (invite is null || invite.IsRevoked)
			return GuildFailures.InviteNotFound;

		if (invite.GuildId != command.GuildId)
			return GuildFailures.InviteGuildMismatch;

		var guild = await guilds.GetByIdWithMembershipAsync(command.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.Members.All(m => m.UserId != currentUser.Id))
			return GuildFailures.NotAMember;

		var isCreator = invite.CreatedBy == currentUser.Id;
		if (!isCreator)
		{
			var mask = PermissionResolver.Resolve(
				currentUser.Id, guild.OwnerId, guild.Roles, guild.MemberRoles);
			if (!PermissionResolver.HasPermission(mask, Permission.ManageGuild))
				return GuildFailures.MissingPermission;
		}

		var revokeResult = invite.Revoke();
		if (revokeResult.IsFailure)
			return revokeResult.Error;

		invites.Update(invite);
		await invites.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
