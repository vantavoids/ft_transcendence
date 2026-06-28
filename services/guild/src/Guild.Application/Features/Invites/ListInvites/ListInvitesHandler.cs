using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
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
		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, query.GuildId, Permission.ManageGuild, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		var rows = await invites.ListByGuildAsync(query.GuildId, cancellationToken);
		IReadOnlyList<InviteDto> dtos = rows.Select(InviteDto.FromEntity).ToList();
		return Result.Ok(dtos);
	}
}
