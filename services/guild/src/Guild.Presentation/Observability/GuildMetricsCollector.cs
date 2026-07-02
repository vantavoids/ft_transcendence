using Guild.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace Guild.Presentation.Observability;

/// <summary>
/// periodically counts the domain tables and pushes the snapshot into
/// <see cref="GuildMetrics"/>. runs off the request/scrape path so the count
/// queries never block a Prometheus scrape; gauges lag reality by at most one
/// interval, which is fine for business trends.
/// </summary>
public sealed class GuildMetricsCollector(
	IServiceScopeFactory scopeFactory,
	GuildMetrics metrics,
	ILogger<GuildMetricsCollector> logger) : BackgroundService
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
				logger.LogWarning(ex, "guild metrics collection failed; keeping the previous snapshot");
			}
		}
		while (await timer.WaitForNextTickAsync(stoppingToken));
	}

	private async Task CollectAsync(CancellationToken ct)
	{
		using var scope = scopeFactory.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<GuildDbContext>();
		var now = DateTimeOffset.UtcNow;

		var guilds = await db.Guilds.CountAsync(ct);
		var members = await db.Members.CountAsync(ct);
		var channels = await db.Channels.CountAsync(ct);
		var roles = await db.Roles.CountAsync(ct);
		var bans = await db.GuildBans.CountAsync(ct);
		var activeInvites = await db.GuildInvites.CountAsync(
			i => !i.IsRevoked
				&& (i.ExpiresAt == null || i.ExpiresAt > now)
				&& (i.MaxUses == null || i.Uses < i.MaxUses), ct);

		metrics.Update(guilds, members, channels, roles, activeInvites, bans);
	}
}
