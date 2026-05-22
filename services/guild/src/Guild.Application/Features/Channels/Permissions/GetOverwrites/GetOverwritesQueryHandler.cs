using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.Permissions.GetOverwrites;

internal sealed class GetOverwritesQueryHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	IChannelPermissionOverwriteRepository overwrites,
	ICurrentUser currentUser)
	: IQueryHandler<GetOverwritesQuery, Result<OverwritesResponse>>
{
	public async Task<Result<OverwritesResponse>> HandleAsync(
		GetOverwritesQuery query,
		CancellationToken cancellationToken = default)
	{
		var channel = await channels.GetByIdAsync(query.ChannelId, cancellationToken);
		if (channel is null)
			return GuildFailures.ChannelNotFound;

		var guild = await guilds.GetByIdWithMembershipAsync(channel.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.Members.All(m => m.UserId != currentUser.Id))
			return GuildFailures.NotAMember;

		var mask = PermissionResolver.Resolve(
			currentUser.Id, guild.OwnerId, guild.Roles, guild.MemberRoles);
		if (!PermissionResolver.HasPermission(mask, Permission.ManageChannels))
			return GuildFailures.MissingPermission;

		var rows = await overwrites.GetForChannelAsync(channel.Id, cancellationToken);
		return new OverwritesResponse([.. rows.Select(OverwriteItem.From)]);
	}
}
