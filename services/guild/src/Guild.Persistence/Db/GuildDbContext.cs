using Guild.Domain.Guild;
using MassTransit;
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
	public DbSet<GuildInvite> GuildInvites => Set<GuildInvite>();
	public DbSet<GuildBan> GuildBans => Set<GuildBan>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.RegisterPostgresEnums();

		// MassTransit transactional (bus) outbox tables. publishing an event now
		// writes an OutboxMessage row inside the same SaveChanges as the business
		// change, so the DB commit and the event can never diverge: a broker
		// outage no longer drops events, and the delivery service ships them once
		// the transaction has committed. see Infrastructure AddEntityFrameworkOutbox
		modelBuilder.AddInboxStateEntity();
		modelBuilder.AddOutboxStateEntity();
		modelBuilder.AddOutboxMessageEntity();

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(GuildDbContext).Assembly);
	}
}
