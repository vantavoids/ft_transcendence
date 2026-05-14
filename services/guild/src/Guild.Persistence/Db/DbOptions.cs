using System.ComponentModel.DataAnnotations;

namespace Guild.Persistence.Db;

public sealed class DbOptions
{
	public const string SectionName = "Database";

	[Required] public required string Host { get; init; }
	[Range(1, 65535)] public int Port { get; init; } = 5432;
	[Required] public required string Name { get; init; }
	[Required] public required string User { get; init; }
	[Required] public required string Password { get; init; }

	public string ToConnectionString()
		=> $"Host={Host};Port={Port};Database={Name};Username={User};Password={Password};Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;Connection Lifetime=0;";
}
