using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Auth.Persistence.Db;

public sealed class AuthDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST")     ?? "localhost";
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT")     ?? "5432";
        var name = Environment.GetEnvironmentVariable("POSTGRES_DB")       ?? "auth";
        var user = Environment.GetEnvironmentVariable("POSTGRES_USER")     ?? "postgres";
        var pass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";

        var conn = $"Host={host};Port={port};Database={name};Username={user};Password={pass}";

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(conn)
            .Options;

        return new AuthDbContext(options);
    }
}
