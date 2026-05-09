using Cassandra;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Chat.Persistence.Health;

internal sealed class ScyllaHealthCheck(ISession session) : IHealthCheck
{
	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var statement = new SimpleStatement("SELECT now() FROM system.local")
				.SetReadTimeoutMillis(2000);
			await session.ExecuteAsync(statement);
			return HealthCheckResult.Healthy();
		}
		catch (Exception ex)
		{
			return HealthCheckResult.Unhealthy("scylla unreachable", ex);
		}
	}
}
