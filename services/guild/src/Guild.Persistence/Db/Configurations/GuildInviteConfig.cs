using Guild.Domain.Guild;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.Persistence.Db.Configurations;

internal sealed class GuildInviteConfig : IEntityTypeConfiguration<GuildInvite>
{
	public void Configure(EntityTypeBuilder<GuildInvite> builder)
	{
		builder.ToTable("guild_invites");

		// optimistic concurrency via Postgres xmin (no DDL). see GuildConfig.
		builder.UseXminConcurrencyToken();

		builder.HasKey(i => i.Code);

		builder.Property(i => i.Code)
			.HasColumnName("code")
			.HasMaxLength(GuildInvite.MaxCodeLen)
			.IsRequired();

		builder.Property(i => i.GuildId).HasColumnName("guild_id").IsRequired();
		builder.Property(i => i.CreatedBy).HasColumnName("created_by").IsRequired();

		builder.Property(i => i.ExpiresAt).HasColumnName("expires_at");
		builder.Property(i => i.MaxUses).HasColumnName("max_uses");

		builder.Property(i => i.Uses)
			.HasColumnName("uses")
			.IsRequired()
			.HasDefaultValue(0);

		builder.Property(i => i.IsRevoked)
			.HasColumnName("is_revoked")
			.IsRequired()
			.HasDefaultValue(false);

		builder.Property(i => i.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("NOW()");

		// matches docs/schema/guild.sql: partial index on non-revoked invites only,
		// since the listing endpoint omits revoked rows and revocation is the
		// vastly more common state long-term
		builder.HasIndex(i => i.GuildId)
			.HasDatabaseName("idx_guild_invites_guild")
			.HasFilter("is_revoked = FALSE");

		// FK back to guilds with ON DELETE CASCADE matches docs/schema/guild.sql.
		// no navigation either side: invites are owned by their own repository,
		// not by the Guild aggregate, and the FK alone is enough for the cascade
		builder.HasOne<GuildEntity>()
			.WithMany()
			.HasForeignKey(i => i.GuildId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
