using Guild.Domain.Guild;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.Persistence.Db.Configurations;

internal sealed class ChannelCategoryConfig : IEntityTypeConfiguration<ChannelCategory>
{
	public void Configure(EntityTypeBuilder<ChannelCategory> builder)
	{
		builder.ToTable("channel_categories");

		builder.HasKey(c => c.Id);
		builder.Property(c => c.Id)
			.HasColumnName("id")
			.ValueGeneratedNever();

		builder.Property(c => c.GuildId)
			.HasColumnName("guild_id")
			.IsRequired();

		builder.Property(c => c.Name)
			.HasColumnName("name")
			.HasMaxLength(ChannelCategory.MaxNameLen)
			.IsRequired();

		builder.Property(c => c.Position)
			.HasColumnName("position")
			.HasDefaultValue(0);

		builder.Property(c => c.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("NOW()");

		builder.Property(c => c.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("NOW()");

		builder.HasOne<GuildEntity>()
			.WithMany()
			.HasForeignKey(c => c.GuildId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(c => new { c.GuildId, c.Position })
			.HasDatabaseName("idx_categories_guild");
	}
}
