using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.ListChannelMembers;

internal sealed class ListChannelMembersHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	IChannelPermissionOverwriteRepository overwrites,
	ICurrentUser currentUser)
	: IQueryHandler<ListChannelMembersQuery, Result<ChannelMembersResponse>>
{
	public async Task<Result<ChannelMembersResponse>> HandleAsync(
		ListChannelMembersQuery query,
		CancellationToken cancellationToken = default)
	{
		var guild = await guilds.GetByIdWithMembershipAsNoTrackingAsync(query.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		// any member of the guild may ask who can read a channel; non-members get 403
		if (guild.Members.All(m => m.UserId != currentUser.Id))
			return GuildFailures.NotAMember;

		var channel = await channels.GetByIdAsync(query.ChannelId, cancellationToken);
		if (channel is null || channel.GuildId != query.GuildId)
			return GuildFailures.ChannelNotFound;

		var channelOverwrites = await overwrites.GetForChannelAsync(query.ChannelId, cancellationToken);
		var readers = ChannelAccess.ReadersOf(guild, query.ChannelId, channelOverwrites);

		return new ChannelMembersResponse([.. readers.Select(id => id.ToString())]);
	}
}
