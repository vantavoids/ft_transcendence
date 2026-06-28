using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Application.Features.Bans.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Bans.ListBans;

internal sealed class ListBansHandler(
	IGuildRepository guilds,
	IGuildBanRepository bans,
	ICurrentUser currentUser)
	: IQueryHandler<ListBansQuery, Result<IReadOnlyList<BanResponse>>>
{
	public async Task<Result<IReadOnlyList<BanResponse>>> HandleAsync(
		ListBansQuery query,
		CancellationToken cancellationToken = default)
	{
		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, query.GuildId, Permission.BanMembers, cancellationToken, asNoTracking: true);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		var page = await bans.ListByGuildAsync(guild.Id, query.After, query.Limit, cancellationToken);
		IReadOnlyList<BanResponse> items = page.Select(BanResponse.From).ToList();
		return Result.Ok(items);
	}
}
