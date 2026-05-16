using System.ComponentModel.DataAnnotations;

namespace Guild.Infrastructure.Options;

internal sealed class SnowflakeOptions : IOptions
{
	static string IOptions.SectionName => "Snowflake";

	[Range(0, 1023)]
	public long WorkerId { get; init; } = 2;

	// 2024-11-12 08:42:00 :D
	public long Epoch { get; init; } = 1731397320000L;
}
