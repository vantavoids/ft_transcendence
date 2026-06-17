using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace Guild.Application.Features.Invites.CleanupExpiredInvites;

internal sealed partial class CleanupExpiredInvitesHandler(IGuildInviteRepository repository, IClock clock, ILogger<CleanupExpiredInvitesHandler> logger) : ICommandHandler<CleanupExpiredInvitesCommand>
{
	private static readonly TimeSpan ExpiredGrace = TimeSpan.FromDays(7);

	public async Task HandleAsync(CleanupExpiredInvitesCommand command, CancellationToken cancellationToken = default)
	{
		var removed = await repository.DeleteRevokedAndExpiredAsync(clock.UtcNow - ExpiredGrace, cancellationToken);
		if (removed > 0)
			LogRemoved(removed);
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "invite cleanup removed {Count} revoked/expired invites")]
	private partial void LogRemoved(int count);
}
