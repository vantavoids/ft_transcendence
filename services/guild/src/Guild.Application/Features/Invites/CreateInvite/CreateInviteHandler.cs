using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
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
	ICurrentUser currentUser)
	: ICommandHandler<CreateInviteCommand, Result<InviteDto>>
{
	public async Task<Result<InviteDto>> HandleAsync(
		CreateInviteCommand command,
		CancellationToken cancellationToken = default)
	{
		var guild = await guilds.GetByIdWithMembershipAsync(command.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.Members.All(m => m.UserId != currentUser.Id))
			return GuildFailures.NotAMember;

		var mask = PermissionResolver.Resolve(
			currentUser.Id, guild.OwnerId, guild.Roles, guild.MemberRoles);
		if (!PermissionResolver.HasPermission(mask, Permission.CreateInvite))
			return GuildFailures.MissingPermission;

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
		await invites.SaveChangesAsync(cancellationToken);

		await eventBus.PublishAsync(
			new GuildInviteCreated(guild.Id, guild.Name, currentUser.Id, InvitedUserId: null),
			cancellationToken);

		return InviteDto.FromEntity(inviteResult.Value);
	}
}
