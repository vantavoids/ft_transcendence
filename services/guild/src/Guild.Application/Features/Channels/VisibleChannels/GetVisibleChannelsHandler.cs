using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Features.Channels.Common;
using Guild.Application.Features.Channels.ListChannels;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.VisibleChannels;

/// <summary>
/// backs Chat Service's <c>GET /channels/read-states</c> sidebar fetch. resolves,
/// for every guild the user belongs to, the channels where their effective
/// permissions include <see cref="Permission.ReadMessages"/>. loads each guild's
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
			var overwritesByChannel = guildOverwrites
				.GroupBy(o => o.ChannelId)
				.ToDictionary(g => g.Key, g => (IReadOnlyList<ChannelPermissionOverwrite>)g.ToList());

			foreach (var channel in guildChannels)
			{
				var channelOverwrites = overwritesByChannel.TryGetValue(channel.Id, out var ows)
					? ows
					: [];
				var permissions = PermissionResolver.Resolve(guild, query.UserId, channelOverwrites);
				if (PermissionResolver.HasPermission(permissions, Permission.ReadMessages))
					visible.Add(ChannelResponse.From(channel));
			}
		}

		return new ChannelListResponse(visible);
	}
}
