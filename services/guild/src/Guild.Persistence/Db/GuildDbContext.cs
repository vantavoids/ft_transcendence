using Microsoft.EntityFrameworkCore;

namespace Guild.Persistence.Db;

public sealed class GuildDbContext(DbContextOptions<GuildDbContext> options) : DbContext(options)
{
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(GuildDbContext).Assembly);
	}
}
