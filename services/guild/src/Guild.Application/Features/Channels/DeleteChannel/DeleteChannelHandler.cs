using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.DeleteChannel;

internal sealed class DeleteChannelHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	ICurrentUser currentUser)
	: ICommandHandler<DeleteChannelCommand, Result>
{
	public async Task<Result> HandleAsync(
		DeleteChannelCommand command,
		CancellationToken cancellationToken = default)
	{
		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, command.GuildId, Permission.ManageChannels, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		var channel = await channels.GetByIdAsync(command.ChannelId, cancellationToken);
		if (channel is null || channel.GuildId != command.GuildId)
			return GuildFailures.ChannelNotFound;

		channels.Remove(channel);
		await channels.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
