using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Invites.CleanupExpiredInvites;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Guild.Persistence.Maintenance;

/// <summary>
/// hosted worker that periodically dispatches <see cref="CleanupExpiredInvitesCommand"/>
/// to hard-delete revoked and past-grace invites. scheduling only: the cleanup
/// policy lives in the command handler. no API surface (see issue #170)
/// </summary>
internal sealed class InviteCleanupService(
	IServiceScopeFactory scopeFactory,
	IOptions<InviteCleanupOptions> options,
	ILogger<InviteCleanupService> logger) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var interval = options.Value.Interval;
		logger.LogInformation("invite cleanup worker started; sweeping every {Interval}", interval);

		using var timer = new PeriodicTimer(interval);

		// wait one interval before the first sweep: nothing accumulates the instant
		// the service boots, and this keeps the worker idle during host startup
		while (await timer.WaitForNextTickAsync(stoppingToken))
			await SweepAsync(stoppingToken);
	}

	private async Task SweepAsync(CancellationToken stoppingToken)
	{
		try
		{
			// the handler is scoped (it owns a DbContext), so each sweep gets a fresh scope
			await using var scope = scopeFactory.CreateAsyncScope();
			var handler = scope.ServiceProvider
				.GetRequiredService<ICommandHandler<CleanupExpiredInvitesCommand>>();
			await handler.HandleAsync(new CleanupExpiredInvitesCommand(), stoppingToken);
		}
		catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
		{
			// shutting down: let the loop's WaitForNextTickAsync observe the cancellation
		}
		catch (DbUpdateException ex)
		{
			// a row lock / write conflict on the invite rows is transient; the next
			// tick retries, so warn rather than error (which implies something broken)
			logger.LogWarning(ex, "invite cleanup sweep hit a transient DB conflict; retrying next tick");
		}
		catch (Exception ex)
		{
			// a failed sweep must not tear down the worker; the next tick retries
			logger.LogError(ex, "invite cleanup sweep failed");
		}
	}
}
