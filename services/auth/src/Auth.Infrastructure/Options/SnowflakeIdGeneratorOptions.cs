using System.ComponentModel.DataAnnotations;

namespace Auth.Infrastructure.Options;

public sealed class SnowFlakeOptions
{
    public const string SectionName = "Snowflake";

    [Range(0, 1023)]
    public long WorkerId { get; init; } = 1;

    public long Epoch { get; init; } = 1704067200000L; // 2024-01-01 00:00:00 UTC in ms
}
