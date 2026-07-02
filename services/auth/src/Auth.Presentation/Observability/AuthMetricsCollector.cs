using Auth.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace Auth.Presentation.Observability;

/// <summary>
/// periodically counts the auth accounts and pushes the snapshot into
/// <see cref="AuthMetrics"/>. runs off the request/scrape path so the count
/// queries never block a Prometheus scrape; gauges lag reality by at most one
/// interval, which is fine for business trends.
/// </summary>
public sealed class AuthMetricsCollector(
	IServiceScopeFactory scopeFactory,
	AuthMetrics metrics,
	ILogger<AuthMetricsCollector> logger) : BackgroundService
{
	private static readonly TimeSpan Interval = TimeSpan.FromSeconds(20);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var timer = new PeriodicTimer(Interval);
		do
		{
			try
			{
				await CollectAsync(stoppingToken);
			}
			catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
			{
				logger.LogWarning(ex, "auth metrics collection failed; keeping the previous snapshot");
			}
		}
		while (await timer.WaitForNextTickAsync(stoppingToken));
	}

	private async Task CollectAsync(CancellationToken ct)
	{
		using var scope = scopeFactory.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
		var now = DateTimeOffset.UtcNow;

		var accounts = await db.AuthUsers.CountAsync(u => u.DeletedAt == null, ct);
		var oauthAccounts = await db.AuthUsers.CountAsync(
			u => u.DeletedAt == null && u.OAuthIdentity != null, ct);
		var activeSessions = await db.AuthUsers.CountAsync(
			u => u.DeletedAt == null
				&& u.RefreshToken != null
				&& !u.RefreshToken.Revoked
				&& u.RefreshToken.ExpiresAt > now, ct);

		metrics.Update(accounts, oauthAccounts, activeSessions);
	}
}
