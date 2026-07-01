using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace Guild.Application.Features.Users.PurgeUserData;

/// <summary>
/// runs the <c>user.deleted</c> GDPR cascade. each step is a single bulk SQL
/// statement (ExecuteDelete / ExecuteUpdate) that bypasses the change tracker, so
/// there is no SaveChanges follow-up. every step is idempotent (deleting or
/// scrubbing already-gone rows is a no-op), which makes the whole handler safe to
/// re-run if the consumer redelivers the event.
/// </summary>
internal sealed partial class PurgeUserDataHandler(
	IGuildRepository guilds,
	IGuildBanRepository bans,
	IGuildInviteRepository invites,
	IChannelPermissionOverwriteRepository overwrites,
	ILogger<PurgeUserDataHandler> logger)
	: ICommandHandler<PurgeUserDataCommand>
{
	public async Task HandleAsync(PurgeUserDataCommand command, CancellationToken cancellationToken = default)
	{
		var userId = command.UserId;

		// subject / sole-artifact rows: remove outright
		await guilds.PurgeMembershipForUserAsync(userId, cancellationToken);
		await bans.RemoveAllForUserAsync(userId, cancellationToken);
		await invites.RemoveAllByCreatorAsync(userId, cancellationToken);
		await overwrites.RemoveAllForMemberAsync(userId, cancellationToken);

		// bans this user issued against others stay in force; only the reference to
		// the now-erased moderator is nulled
		await bans.ScrubModeratorAsync(userId, cancellationToken);

		LogPurged(userId);
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "purged Guild data for deleted user {UserId}")]
	private partial void LogPurged(long userId);
}
