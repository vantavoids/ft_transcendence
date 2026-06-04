using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Features.Invites.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Invites.ListInvites;

internal sealed class ListInvitesHandler(
	IGuildRepository guilds,
	IGuildInviteRepository invites,
	ICurrentUser currentUser)
	: IQueryHandler<ListInvitesQuery, Result<IReadOnlyList<InviteDto>>>
{
	public async Task<Result<IReadOnlyList<InviteDto>>> HandleAsync(
		ListInvitesQuery query,
		CancellationToken cancellationToken = default)
	{
		var guild = await guilds.GetByIdWithMembershipAsync(query.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.Members.All(m => m.UserId != currentUser.Id))
			return GuildFailures.NotAMember;

		var mask = PermissionResolver.Resolve(
			currentUser.Id, guild.OwnerId, guild.Roles, guild.MemberRoles);
		if (!PermissionResolver.HasPermission(mask, Permission.ManageGuild))
			return GuildFailures.MissingPermission;

		var rows = await invites.ListByGuildAsync(query.GuildId, cancellationToken);
		IReadOnlyList<InviteDto> dtos = rows.Select(InviteDto.FromEntity).ToList();
		return Result.Ok(dtos);
	}
}
