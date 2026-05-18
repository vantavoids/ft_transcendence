using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.Membership;

internal sealed class GetChannelMembershipHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	IChannelPermissionOverwriteRepository overwrites)
	: IQueryHandler<GetChannelMembershipQuery, Result<MembershipResponse>>
{
	public async Task<Result<MembershipResponse>> HandleAsync(
		GetChannelMembershipQuery query,
		CancellationToken cancellationToken = default)
	{
		var channel = await channels.GetByIdAsync(query.ChannelId, cancellationToken);
		if (channel is null)
			return GuildFailures.ChannelNotFound;

		var guild = await guilds.GetByIdWithMembershipAsync(channel.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		var isMember = guild.Members.Any(m => m.UserId == query.UserId);
		if (!isMember)
			return new MembershipResponse(IsMember: false, GuildId: channel.GuildId.ToString(), Permissions: 0L);

		var channelOverwrites = await overwrites.GetForChannelAsync(channel.Id, cancellationToken);
		var permissions = PermissionResolver.Resolve(guild, query.UserId, channel, channelOverwrites);

		return new MembershipResponse(
			IsMember: true,
			GuildId: channel.GuildId.ToString(),
			Permissions: permissions);
	}
}
