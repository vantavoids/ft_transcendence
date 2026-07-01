using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Domain.Results;

namespace Guild.Application.Features.Guilds.ListMyGuilds;

internal sealed class ListMyGuildsHandler(
	IGuildRepository guilds,
	ICurrentUser currentUser)
	: IQueryHandler<ListMyGuildsQuery, Result<MyGuildListResponse>>
{
	public async Task<Result<MyGuildListResponse>> HandleAsync(
		ListMyGuildsQuery query,
		CancellationToken cancellationToken = default)
	{
		var summaries = await guilds.ListForMemberAsync(currentUser.Id, cancellationToken);
		var dtos = summaries.Select(MyGuildDto.From).ToList();
		return new MyGuildListResponse(dtos);
	}
}
