using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
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

		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, channel.GuildId, Permission.ManageChannels, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		var rows = await overwrites.GetForChannelAsync(channel.Id, cancellationToken);
		return new OverwritesResponse([.. rows.Select(OverwriteItem.From)]);
	}
}
