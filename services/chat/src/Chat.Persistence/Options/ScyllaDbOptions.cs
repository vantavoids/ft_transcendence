using System.ComponentModel.DataAnnotations;

namespace Chat.Persistence.Options;

public sealed class ScyllaDbOptions
{
	[Required] public required string ContactPoints { get; init; }
	[Required] public required string Keyspace { get; init; }
	public string? Username { get; init; }
	public string? Password { get; init; }
	public int Port { get; init; } = 9042;
}
