using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Guild.Persistence.Db;

public sealed class GuildDbContextDesignTimeFactory : IDesignTimeDbContextFactory<GuildDbContext>
{
	public GuildDbContext CreateDbContext(string[] args)
	{
		var configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.local.json", optional: true)
			.AddEnvironmentVariables()
			.Build();

		var options = configuration.GetSection(DbOptions.SectionName).Get<DbOptions>()
			?? throw new InvalidOperationException(
				$"Missing '{DbOptions.SectionName}' configuration. " +
				"Set Database__Host / Database__Port / Database__Name / Database__User / Database__Password.");

		var dbContextOptions = new DbContextOptionsBuilder<GuildDbContext>()
			.UseNpgsql(options.ToConnectionString(), o => o.MapPostgresEnums())
			.Options;

		return new GuildDbContext(dbContextOptions);
	}
}
