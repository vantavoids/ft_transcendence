using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Abstractions.Users;
using Guild.Application.Contracts;
using Guild.Application.Features.Guilds.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.JoinByInviteCode;

internal sealed class JoinByInviteCodeHandler(
	IGuildRepository guilds,
	IGuildInviteRepository invites,
	IEventBus eventBus,
	IUserService users,
	IClock clock,
	ICurrentUser currentUser)
	: ICommandHandler<JoinByInviteCodeCommand, Result<GuildDto>>
{
	public async Task<Result<GuildDto>> HandleAsync(
		JoinByInviteCodeCommand command,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(command.Code))
			return GuildFailures.InviteCodeRequired;

		var invite = await invites.GetByCodeAsync(command.Code, cancellationToken);
		if (invite is null)
			return GuildFailures.InviteNotFound;

		if (command.ExpectedGuildId is { } expected && invite.GuildId != expected)
			return GuildFailures.InviteGuildMismatch;

		var now = clock.UtcNow;

		// fail fast before loading the guild aggregate: revoked / expired /
		// exhausted invites get rejected without a second DB roundtrip, and the
		// error ordering matches how a user thinks ("the code itself is bad",
		// not "you're already a member of the guild this dead code points to")
		if (!invite.IsActive(now))
			return GuildFailures.InviteUnusable;

		var guild = await guilds.GetByIdWithMembershipAsync(invite.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.Members.Any(m => m.UserId == currentUser.Id))
			return GuildFailures.AlreadyMember;

		var consumeResult = invite.Consume(now);
		if (consumeResult.IsFailure)
			return consumeResult.Error;

		var addResult = guild.AddMember(currentUser.Id, now);
		if (addResult.IsFailure)
			return addResult.Error;

		// best-effort existence check; logs internally on failure but does not block
		await users.ExistsAsync(currentUser.Id, cancellationToken);

		invites.Update(invite);
		guilds.Update(guild);
		await guilds.SaveChangesAsync(cancellationToken);

		await eventBus.PublishAsync(
			new GuildMemberJoined(guild.Id, guild.Name, currentUser.Id),
			cancellationToken);

		return GuildDto.FromEntity(guild);
	}
}
