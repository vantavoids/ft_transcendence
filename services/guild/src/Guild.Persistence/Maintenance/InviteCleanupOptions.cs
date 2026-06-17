using System.ComponentModel.DataAnnotations;

namespace Guild.Persistence.Maintenance;

public sealed class InviteCleanupOptions
{
	public const string SectionName = "InviteCleanup";

	/// <summary>
	/// how often the worker runs a cleanup sweep. defaults to hourly. bound from
	/// config as a TimeSpan string (e.g. "01:00:00"). must be positive — a zero or
	/// negative interval would make <see cref="PeriodicTimer"/> throw at startup
	/// </summary>
	[Range(typeof(TimeSpan), "00:01:00", "7.00:00:00")]
	public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);
}
