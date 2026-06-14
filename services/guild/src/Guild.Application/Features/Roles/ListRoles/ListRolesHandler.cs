using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Features.Roles.Common;
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
		var guild = await guilds.GetByIdWithMembershipAsync(query.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.Members.All(m => m.UserId != currentUser.Id))
			return GuildFailures.NotAMember;

		var items = guild.Roles
			.OrderBy(r => r.Position)
			.Select(RoleResponse.From)
			.ToList();

		return new RoleListResponse(items);
	}
}
