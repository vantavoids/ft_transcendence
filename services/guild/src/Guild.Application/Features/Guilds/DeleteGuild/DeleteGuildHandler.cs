using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Contracts;
using Guild.Domain.Results;

namespace Guild.Application.Features.Guilds.DeleteGuild;

internal sealed class DeleteGuildHandler(
	IGuildRepository repository,
	IChannelRepository channels,
	IEventBus eventBus,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
	: ICommandHandler<DeleteGuildCommand, Result>
{
	public async Task<Result> HandleAsync(
		DeleteGuildCommand command,
		CancellationToken cancellationToken = default)
	{
		var guild = await repository.GetByIdAsync(command.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.OwnerId != currentUser.Id)
			return GuildFailures.NotTheOwner;

		// capture the channel ids before the cascade removes them, so Chat can
		// purge each channel's message history it stores in ScyllaDB.
		var guildChannels = await channels.GetByGuildAsync(command.GuildId, cancellationToken);
		var channelIds = guildChannels.Select(c => c.Id).ToList();

		repository.Remove(guild);

		// publish before SaveChanges so the outbox binds it to the same transaction
		await eventBus.PublishAsync(new GuildDeleted(guild.Id, channelIds), cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
