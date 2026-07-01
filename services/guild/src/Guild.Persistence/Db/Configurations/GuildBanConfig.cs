using Guild.Domain.Guild;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.Persistence.Db.Configurations;

internal sealed class GuildBanConfig : IEntityTypeConfiguration<GuildBan>
{
	public void Configure(EntityTypeBuilder<GuildBan> builder)
	{
		builder.ToTable("guild_bans");

		// optimistic concurrency via Postgres xmin (no DDL). see GuildConfig.
		// NOTE: xmin only lands in the WHERE of UPDATE/DELETE, so it guards a
		// concurrent unban (DELETE) race, NOT the insert race. a concurrent
		// double-ban is two INSERTs on the composite PK; the loser raises a
		// 23505 unique violation (a DbUpdateException, not a
		// DbUpdateConcurrencyException) that the global exception handler maps
		// to a 409 instead of a 500.
		builder.UseXminConcurrencyToken();

		builder.HasKey(b => new { b.GuildId, b.UserId });

		builder.Property(b => b.GuildId).HasColumnName("guild_id").IsRequired();
		builder.Property(b => b.UserId).HasColumnName("user_id").IsRequired();
		// nullable: scrubbed to null by the user.deleted cascade (see GuildBan.BannedBy)
		builder.Property(b => b.BannedBy).HasColumnName("banned_by");

		builder.Property(b => b.Reason)
			.HasColumnName("reason")
			.HasMaxLength(GuildBan.MaxReasonLen);

		builder.Property(b => b.BannedAt)
			.HasColumnName("banned_at")
			.IsRequired()
			.HasDefaultValueSql("NOW()");

		// matches docs/schema/guild.sql: secondary index on user_id for the
		// future user.deleted consumer and any "list all bans for this user"
		// internal query
		builder.HasIndex(b => b.UserId)
			.HasDatabaseName("idx_guild_bans_user");

		// FK back to guilds with ON DELETE CASCADE matches docs/schema/guild.sql.
		// no navigation either side: bans are owned by their own repository,
		// not by the Guild aggregate, and the FK alone is enough for the cascade
		builder.HasOne<GuildEntity>()
			.WithMany()
			.HasForeignKey(b => b.GuildId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
