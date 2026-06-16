using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Features.Bans.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Bans.ListBans;

internal sealed class ListBansHandler(
	IGuildRepository guilds,
	IGuildBanRepository bans,
	ICurrentUser currentUser)
	: IQueryHandler<ListBansQuery, Result<BanListResponse>>
{
	public async Task<Result<BanListResponse>> HandleAsync(
		ListBansQuery query,
		CancellationToken cancellationToken = default)
	{
		var guild = await guilds.GetByIdWithMembershipAsync(query.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.Members.All(m => m.UserId != currentUser.Id))
			return GuildFailures.NotAMember;

		var mask = PermissionResolver.Resolve(
			currentUser.Id, guild.OwnerId, guild.Roles, guild.MemberRoles);
		if (!PermissionResolver.HasPermission(mask, Permission.BanMembers))
			return GuildFailures.MissingPermission;

		var page = await bans.ListByGuildAsync(guild.Id, query.After, query.Limit, cancellationToken);
		return new BanListResponse(page.Select(BanResponse.From).ToList());
	}
}
