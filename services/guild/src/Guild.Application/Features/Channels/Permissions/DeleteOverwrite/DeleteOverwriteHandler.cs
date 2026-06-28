using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.Permissions.DeleteOverwrite;

internal sealed class DeleteOverwriteHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	IChannelPermissionOverwriteRepository overwrites,
	ICurrentUser currentUser)
	: ICommandHandler<DeleteOverwriteCommand, Result>
{
	public async Task<Result> HandleAsync(
		DeleteOverwriteCommand command,
		CancellationToken cancellationToken = default)
	{
		var channel = await channels.GetByIdAsync(command.ChannelId, cancellationToken);
		if (channel is null)
			return GuildFailures.ChannelNotFound;

		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, channel.GuildId, Permission.ManageChannels, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		// the URL carries only (channel_id, target_id); since snowflake ids are
		// unique across roles and members, a single server-side probe by
		// (channel_id, target_id) finds the row regardless of target_type
		var match = await overwrites.GetForChannelByTargetIdAsync(
			channel.Id, command.TargetId, cancellationToken);
		if (match is null)
			return GuildFailures.OverwriteNotFound;

		overwrites.Remove(match);
		await overwrites.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
