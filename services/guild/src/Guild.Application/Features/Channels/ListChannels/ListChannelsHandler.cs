using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Features.Channels.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.ListChannels;

internal sealed class ListChannelsHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	ICurrentUser currentUser)
	: IQueryHandler<ListChannelsQuery, Result<ChannelListResponse>>
{
	public async Task<Result<ChannelListResponse>> HandleAsync(
		ListChannelsQuery query,
		CancellationToken cancellationToken = default)
	{
		var guild = await guilds.GetByIdAsync(query.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		var isMember = await guilds.IsMemberAsync(query.GuildId, currentUser.Id, cancellationToken);
		if (!isMember)
			return GuildFailures.NotAMember;

		var entities = await channels.GetByGuildAsync(query.GuildId, cancellationToken);
		var dtos = entities.Select(ChannelResponse.From).ToList();
		return new ChannelListResponse(dtos);
	}
}
