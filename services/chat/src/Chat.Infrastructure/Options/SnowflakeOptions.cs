using System.ComponentModel.DataAnnotations;

namespace Chat.Infrastructure.Options;

public sealed class SnowflakeOptions
{
	[Range(0, 1023)]
	public long WorkerId { get; init; } = 3;

	// 2024-11-12 08:42:00 UTC
	public long Epoch { get; init; } = 1731397320000L;
}
