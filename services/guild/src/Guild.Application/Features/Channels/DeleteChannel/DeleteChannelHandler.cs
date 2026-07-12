using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Application.Contracts;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.DeleteChannel;

internal sealed class DeleteChannelHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	IChannelPermissionOverwriteRepository overwrites,
	IEventBus eventBus,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
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

		// resolve who could see the channel *before* removing it, so Chat can tell
		// exactly those members to drop it from their sidebar.
		var channelOverwrites = await overwrites.GetForChannelAsync(channel.Id, cancellationToken);
		var eligible = ChannelAccess.ReadersOf(guild, channel.Id, channelOverwrites);

		channels.Remove(channel);

		await eventBus.PublishAsync(
			new GuildChannelDeleted(command.GuildId, channel.Id, eligible),
			cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
