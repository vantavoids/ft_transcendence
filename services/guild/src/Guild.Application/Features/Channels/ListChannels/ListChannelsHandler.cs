using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Features.Channels.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.ListChannels;

internal sealed class ListChannelsHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	IChannelPermissionOverwriteRepository overwrites,
	ICurrentUser currentUser)
	: IQueryHandler<ListChannelsQuery, Result<ChannelListResponse>>
{
	public async Task<Result<ChannelListResponse>> HandleAsync(
		ListChannelsQuery query,
		CancellationToken cancellationToken = default)
	{
		var guild = await guilds.GetByIdWithMembershipAsNoTrackingAsync(query.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.Members.All(m => m.UserId != currentUser.Id))
			return GuildFailures.NotAMember;

		var entities = await channels.GetByGuildAsync(query.GuildId, cancellationToken);
		var guildOverwrites = await overwrites.GetForGuildAsync(query.GuildId, cancellationToken);
		var overwritesByChannel = guildOverwrites
			.GroupBy(o => o.ChannelId)
			.ToDictionary(g => g.Key, g => (IReadOnlyList<ChannelPermissionOverwrite>)g.ToList());

		// list only channels the caller can actually read: a READ_CHANNEL deny
		// overwrite (on the member or one of their roles) hides the channel here,
		// mirroring GetVisibleChannelsHandler so the sidebar no longer shows a
		// channel the member was denied. owners/admins short-circuit to all
		// permissions in the resolver, so management access is unaffected.
		var dtos = entities
			.Where(channel =>
			{
				var channelOverwrites = overwritesByChannel.TryGetValue(channel.Id, out var ows) ? ows : [];
				var permissions = PermissionResolver.Resolve(guild, currentUser.Id, channelOverwrites);
				return PermissionResolver.HasPermission(permissions, Permission.ReadMessages);
			})
			.Select(ChannelResponse.From)
			.ToList();

		return new ChannelListResponse(dtos);
	}
}
