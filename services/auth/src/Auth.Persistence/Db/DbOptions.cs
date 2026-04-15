using System.ComponentModel.DataAnnotations;

namespace Auth.Persistence.Db;

public sealed class DbOptions
{
    public const string SectionName = "Database";

    [Required] public required string Host { get; init; }
    [Range(1, 65535)] public int      Port { get; init; }
    [Required] public required string Name { get; init; }
    [Required] public required string User { get; init; }
    [Required] public required string Password { get; init; }

    public DbOptions()
    {
        Host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "postgres";
        Port = int.Parse(Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432");
        Name = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "auth_db";
        User = Environment.GetEnvironmentVariable("POSTGRES_USER")
            ?? throw new InvalidOperationException("POSTGRES_USER is required");
        Password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")
            ?? throw new InvalidOperationException("POSTGRES_PASSWORD is required");
    }

    public string ToConnectionString()
        => $"Host={Host};Port={Port};Database={Name};Username={User};Password={Password};Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;Connection Lifetime=0;";
}
