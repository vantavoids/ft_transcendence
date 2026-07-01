using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Contracts;
using Guild.Application.Features.Guilds.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Guilds.TransferOwnership;

internal sealed class TransferOwnershipHandler(
	IGuildRepository repository,
	IEventBus eventBus,
	IClock clock,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
	: ICommandHandler<TransferOwnershipCommand, Result<GuildDto>>
{
	public async Task<Result<GuildDto>> HandleAsync(
		TransferOwnershipCommand command,
		CancellationToken cancellationToken = default)
	{
		var guild = await repository.GetByIdAsync(command.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.OwnerId != currentUser.Id)
			return GuildFailures.NotTheOwner;

		var targetIsMember = await repository.IsMemberAsync(
			command.GuildId,
			command.NewOwnerId,
			cancellationToken);
		if (!targetIsMember)
			return GuildFailures.TargetNotAMember;

		var previousOwnerId = guild.OwnerId;

		var transferResult = guild.TransferOwnership(command.NewOwnerId, clock.UtcNow);
		if (transferResult.IsFailure)
			return transferResult.Error;

		await eventBus.PublishAsync(
			new GuildOwnerTransferred(guild.Id, previousOwnerId, command.NewOwnerId),
			cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return GuildDto.FromEntity(guild);
	}
}
