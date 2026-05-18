using Guild.Domain.Guild;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.Persistence.Db.Configurations;

internal sealed class ChannelConfig : IEntityTypeConfiguration<Channel>
{
	public void Configure(EntityTypeBuilder<Channel> builder)
	{
		builder.ToTable("channels");

		builder.HasKey(c => c.Id);
		builder.Property(c => c.Id)
			.HasColumnName("id")
			.ValueGeneratedNever();

		builder.Property(c => c.GuildId)
			.HasColumnName("guild_id")
			.IsRequired();

		builder.Property(c => c.CategoryId)
			.HasColumnName("category_id");

		builder.Property(c => c.Name)
			.HasColumnName("name")
			.HasMaxLength(Channel.MaxNameLen)
			.IsRequired();

		builder.Property(c => c.Topic)
			.HasColumnName("topic")
			.HasColumnType("text");

		builder.Property(c => c.Type)
			.HasColumnName("type")
			.HasConversion<int>()
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

		builder.HasOne<ChannelCategory>()
			.WithMany()
			.HasForeignKey(c => c.CategoryId)
			.OnDelete(DeleteBehavior.SetNull);

		// composite index for the (guild, category, position) lookup used by
		// listing and auto-append-on-create
		builder.HasIndex(c => new { c.GuildId, c.CategoryId, c.Position })
			.HasDatabaseName("idx_channels_guild_category_position");
	}
}
