using System.Diagnostics.Metrics;

namespace Auth.Presentation.Observability;

/// <summary>
/// domain gauges for Auth (account and active-session totals). the values are
/// refreshed off the scrape path by <see cref="AuthMetricsCollector"/>; the
/// observable gauges just read the last cached snapshot, so a Prometheus scrape
/// never triggers a database query.
/// </summary>
public sealed class AuthMetrics : IDisposable
{
	public const string MeterName = "Auth.Domain";

	private readonly Meter _meter;
	private long _accounts, _oauthAccounts, _activeSessions;

	public AuthMetrics(IMeterFactory factory)
	{
		_meter = factory.Create(MeterName);
		_meter.CreateObservableGauge("auth.accounts", () => Volatile.Read(ref _accounts), description: "Live accounts (not deleted)");
		_meter.CreateObservableGauge("auth.accounts.oauth", () => Volatile.Read(ref _oauthAccounts), description: "Live accounts backed by an OAuth identity");
		_meter.CreateObservableGauge("auth.sessions.active", () => Volatile.Read(ref _activeSessions), description: "Accounts with a valid, non-revoked refresh token");
	}

	public void Update(long accounts, long oauthAccounts, long activeSessions)
	{
		Volatile.Write(ref _accounts, accounts);
		Volatile.Write(ref _oauthAccounts, oauthAccounts);
		Volatile.Write(ref _activeSessions, activeSessions);
	}

	public void Dispose() => _meter.Dispose();
}
