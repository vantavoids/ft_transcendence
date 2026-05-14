using Guild.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Guild.Persistence.Health;

internal sealed class EfCoreHealthCheck(GuildDbContext db) : IHealthCheck
{
	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var canConnect = await db.Database.CanConnectAsync(cancellationToken);
			return canConnect
				? HealthCheckResult.Healthy()
				: HealthCheckResult.Unhealthy("postgres unreachable");
		}
		catch (Exception ex)
		{
			return HealthCheckResult.Unhealthy("postgres unreachable", ex);
		}
	}
}
