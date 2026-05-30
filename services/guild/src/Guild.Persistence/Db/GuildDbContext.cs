using Guild.Domain.Guild;
using Microsoft.EntityFrameworkCore;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.Persistence.Db;

public sealed class GuildDbContext(DbContextOptions<GuildDbContext> options) : DbContext(options)
{
	public DbSet<GuildEntity> Guilds => Set<GuildEntity>();
	public DbSet<Role> Roles => Set<Role>();
	public DbSet<GuildMember> Members => Set<GuildMember>();
	public DbSet<MemberRole> MemberRoles => Set<MemberRole>();
	public DbSet<ChannelCategory> ChannelCategories => Set<ChannelCategory>();
	public DbSet<Channel> Channels => Set<Channel>();
	public DbSet<ChannelPermissionOverwrite> ChannelPermissionOverwrites => Set<ChannelPermissionOverwrite>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.RegisterPostgresEnums();
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(GuildDbContext).Assembly);
	}
}
