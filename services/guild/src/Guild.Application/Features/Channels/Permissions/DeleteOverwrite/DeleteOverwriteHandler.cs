using Guild.Application.Abstractions;
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
	IEventBus eventBus,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
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

		var channelOverwrites = await overwrites.GetForChannelAsync(channel.Id, cancellationToken);
		// the URL carries only (channel_id, target_id); snowflake ids are unique
		// across roles and members, so target_id alone finds the row
		var match = channelOverwrites.FirstOrDefault(o => o.TargetId == command.TargetId);
		if (match is null)
			return GuildFailures.OverwriteNotFound;

		var snapshot = ChannelAccess.CaptureForChannel(
			guild, channel.Id,
			ChannelAccess.MembersAffectedBy(guild, match.TargetType, match.TargetId),
			channelOverwrites);

		overwrites.Remove(match);

		var after = channelOverwrites.Where(o => o.Id != match.Id).ToList();
		await ChannelAccess.PublishRevocationsAsync(eventBus, guild, snapshot, channel.Id, after, cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
