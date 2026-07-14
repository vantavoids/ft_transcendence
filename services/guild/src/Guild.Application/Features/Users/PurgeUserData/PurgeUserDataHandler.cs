using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace Guild.Application.Features.Users.PurgeUserData;

/// <summary>
/// runs the <c>user.deleted</c> GDPR cascade. each purge step is a single bulk SQL
/// statement (ExecuteDelete / ExecuteUpdate) that bypasses the change tracker.
/// every step is idempotent (deleting or scrubbing already-gone rows is a no-op),
/// which makes the whole handler safe to re-run if the consumer redelivers the
/// event. it also emits a <see cref="GuildMemberLeft"/> per guild the user
/// belonged to so Chat drops their SignalR groups and remaining members' rosters
/// update live, exactly as a normal leave/kick would.
/// </summary>
internal sealed partial class PurgeUserDataHandler(
	IGuildRepository guilds,
	IGuildBanRepository bans,
	IGuildInviteRepository invites,
	IChannelPermissionOverwriteRepository overwrites,
	IEventBus eventBus,
	IUnitOfWork unitOfWork,
	ILogger<PurgeUserDataHandler> logger)
	: ICommandHandler<PurgeUserDataCommand>
{
	public async Task HandleAsync(PurgeUserDataCommand command, CancellationToken cancellationToken = default)
	{
		var userId = command.UserId;

		// capture the user's guilds before the purge removes their membership, so
		// we can broadcast a member_left for each one
		var memberGuildIds = await guilds.ListGuildIdsForMemberAsync(userId, cancellationToken);

		// subject / sole-artifact rows: remove outright
		await guilds.PurgeMembershipForUserAsync(userId, cancellationToken);
		await bans.RemoveAllForUserAsync(userId, cancellationToken);
		await invites.RemoveAllByCreatorAsync(userId, cancellationToken);
		await overwrites.RemoveAllForMemberAsync(userId, cancellationToken);

		// bans this user issued against others stay in force; only the reference to
		// the now-erased moderator is nulled
		await bans.ScrubModeratorAsync(userId, cancellationToken);

		// same fan-out a leave/kick produces; SaveChanges below flushes the bus
		// outbox (the purge steps above are bulk deletes that do not touch it)
		foreach (var guildId in memberGuildIds)
			await eventBus.PublishAsync(new GuildMemberLeft(guildId, userId), cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		LogPurged(userId);
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "purged Guild data for deleted user {UserId}")]
	private partial void LogPurged(long userId);
}
