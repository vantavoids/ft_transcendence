using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Domain.Results;

namespace Guild.Application.Features.Guilds.UserGuildIds;

/// <summary>
/// internal lookup used by the Chat Service on hub connect to subscribe a
/// connection to the <c>guild:{id}</c> SignalR group of every guild the user
/// belongs to, so guild-scoped real-time broadcasts (channel/member/role
/// changes, presence) reach all members without a per-guild round trip.
/// </summary>
internal sealed class GetUserGuildIdsHandler(IGuildRepository guilds)
	: IQueryHandler<GetUserGuildIdsQuery, Result<IReadOnlyList<long>>>
{
	public async Task<Result<IReadOnlyList<long>>> HandleAsync(
		GetUserGuildIdsQuery query,
		CancellationToken cancellationToken = default)
	{
		var guildIds = await guilds.ListGuildIdsForMemberAsync(query.UserId, cancellationToken);
		return Result.Ok(guildIds);
	}
}
