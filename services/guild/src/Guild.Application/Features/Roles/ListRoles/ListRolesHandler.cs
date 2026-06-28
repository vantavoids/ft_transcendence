using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Application.Features.Roles.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Roles.ListRoles;

internal sealed class ListRolesHandler(
	IGuildRepository guilds,
	ICurrentUser currentUser)
	: IQueryHandler<ListRolesQuery, Result<RoleListResponse>>
{
	public async Task<Result<RoleListResponse>> HandleAsync(
		ListRolesQuery query,
		CancellationToken cancellationToken = default)
	{
		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, query.GuildId, Permission.None, cancellationToken, asNoTracking: true);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		var items = guild.Roles
			.OrderBy(r => r.Position)
			.Select(RoleResponse.From)
			.ToList();

		return new RoleListResponse(items);
	}
}
