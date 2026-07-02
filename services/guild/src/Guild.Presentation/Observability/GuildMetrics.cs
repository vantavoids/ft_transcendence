using System.Diagnostics.Metrics;

namespace Guild.Presentation.Observability;

/// <summary>
/// domain gauges for Guild (guild/member/channel/role/invite/ban totals). the
/// values are refreshed off the scrape path by <see cref="GuildMetricsCollector"/>;
/// the observable gauges just read the last cached snapshot, so a Prometheus
/// scrape never triggers a database query.
/// </summary>
public sealed class GuildMetrics : IDisposable
{
	public const string MeterName = "Guild.Domain";

	private readonly Meter _meter;
	private long _guilds, _members, _channels, _roles, _activeInvites, _bans;

	public GuildMetrics(IMeterFactory factory)
	{
		_meter = factory.Create(MeterName);
		_meter.CreateObservableGauge("guild.guilds", () => Volatile.Read(ref _guilds), description: "Guilds");
		_meter.CreateObservableGauge("guild.members", () => Volatile.Read(ref _members), description: "Guild memberships");
		_meter.CreateObservableGauge("guild.channels", () => Volatile.Read(ref _channels), description: "Channels");
		_meter.CreateObservableGauge("guild.roles", () => Volatile.Read(ref _roles), description: "Roles");
		_meter.CreateObservableGauge("guild.invites.active", () => Volatile.Read(ref _activeInvites), description: "Invites that are not revoked, expired, or used up");
		_meter.CreateObservableGauge("guild.bans", () => Volatile.Read(ref _bans), description: "Active bans");
	}

	public void Update(long guilds, long members, long channels, long roles, long activeInvites, long bans)
	{
		Volatile.Write(ref _guilds, guilds);
		Volatile.Write(ref _members, members);
		Volatile.Write(ref _channels, channels);
		Volatile.Write(ref _roles, roles);
		Volatile.Write(ref _activeInvites, activeInvites);
		Volatile.Write(ref _bans, bans);
	}

	public void Dispose() => _meter.Dispose();
}
