using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Authorization;
using Guild.Application.Features.Channels.Common;
using Guild.Application.Features.Channels.ListChannels;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.VisibleChannels;

/// <summary>
/// backs Chat Service's <c>GET /channels/read-states</c> sidebar fetch. resolves,
/// for every guild the user belongs to, the channels where their effective
/// permissions include <see cref="Guild.Domain.Guild.Permission.ReadMessages"/>. loads each guild's
/// membership/roles, channels and overwrites via the existing lean per-guild
/// reads (no full-member hydration, no N+1 across guilds) rather than looping
/// the per-channel membership check.
/// </summary>
internal sealed class GetVisibleChannelsHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	IChannelPermissionOverwriteRepository overwrites)
	: IQueryHandler<GetVisibleChannelsQuery, Result<ChannelListResponse>>
{
	public async Task<Result<ChannelListResponse>> HandleAsync(
		GetVisibleChannelsQuery query,
		CancellationToken cancellationToken = default)
	{
		var guildIds = await guilds.ListGuildIdsForMemberAsync(query.UserId, cancellationToken);

		var visible = new List<ChannelResponse>();
		foreach (var guildId in guildIds)
		{
			var guild = await guilds.GetByIdWithMembershipAsNoTrackingAsync(guildId, cancellationToken);
			if (guild is null)
				continue;

			var guildChannels = await channels.GetByGuildAsync(guildId, cancellationToken);
			var guildOverwrites = await overwrites.GetForGuildAsync(guildId, cancellationToken);
			var overwritesByChannel = ChannelAccess.GroupByChannel(guildOverwrites);

			foreach (var channel in ChannelAccess.ReadableChannels(guild, query.UserId, guildChannels, overwritesByChannel))
				visible.Add(ChannelResponse.From(channel));
		}

		return new ChannelListResponse(visible);
	}
}
