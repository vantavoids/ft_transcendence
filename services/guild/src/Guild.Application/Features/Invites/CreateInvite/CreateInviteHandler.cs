using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Application.Contracts;
using Guild.Application.Features.Invites.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Invites.CreateInvite;

internal sealed class CreateInviteHandler(
	IGuildRepository guilds,
	IGuildInviteRepository invites,
	IInviteCodeGenerator codes,
	IEventBus eventBus,
	IClock clock,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
	: ICommandHandler<CreateInviteCommand, Result<InviteDto>>
{
	public async Task<Result<InviteDto>> HandleAsync(
		CreateInviteCommand command,
		CancellationToken cancellationToken = default)
	{
		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, command.GuildId, Permission.CreateInvite, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		var now = clock.UtcNow;
		var expiresAt = command.ExpiresInHours is { } hours
			? now.AddHours(hours)
			: (DateTimeOffset?)null;

		var inviteResult = GuildInvite.Create(
			code: codes.NextCode(),
			guildId: command.GuildId,
			createdBy: currentUser.Id,
			maxUses: command.MaxUses,
			expiresAt: expiresAt,
			now: now);
		if (inviteResult.IsFailure)
			return inviteResult.Error;

		await invites.AddAsync(inviteResult.Value, cancellationToken);

		// publish BEFORE SaveChanges so the bus outbox binds the GuildInviteCreated
		// event to the same transaction as the invite insert
		await eventBus.PublishAsync(
			new GuildInviteCreated(guild.Id, guild.Name, currentUser.Id, InvitedUserId: null),
			cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return InviteDto.FromEntity(inviteResult.Value);
	}
}
