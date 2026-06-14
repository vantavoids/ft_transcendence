using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Domain.Results;

namespace Guild.Application.Features.Guilds.CountOwnedGuilds;

internal sealed class CountOwnedGuildsHandler(IGuildRepository guilds)
	: IQueryHandler<CountOwnedGuildsQuery, Result<OwnedGuildsCountResponse>>
{
	public async Task<Result<OwnedGuildsCountResponse>> HandleAsync(
		CountOwnedGuildsQuery query,
		CancellationToken cancellationToken = default)
	{
		var count = await guilds.CountOwnedByAsync(query.UserId, cancellationToken);
		return new OwnedGuildsCountResponse(count);
	}
}
