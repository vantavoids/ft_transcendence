using Guild.Domain.Guild;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guild.Persistence.Db.Configurations;

internal sealed class ChannelPermissionOverwriteConfig : IEntityTypeConfiguration<ChannelPermissionOverwrite>
{
	public void Configure(EntityTypeBuilder<ChannelPermissionOverwrite> builder)
	{
		builder.ToTable("channel_permission_overwrites");

		builder.HasKey(o => o.Id);
		builder.Property(o => o.Id)
			.HasColumnName("id")
			.ValueGeneratedNever();

		builder.Property(o => o.ChannelId)
			.HasColumnName("channel_id")
			.IsRequired();

		builder.Property(o => o.TargetType)
			.HasColumnName("target_type")
			.HasConversion<int>()
			.IsRequired();

		builder.Property(o => o.TargetId)
			.HasColumnName("target_id")
			.IsRequired();

		builder.Property(o => o.Allow)
			.HasColumnName("allow")
			.HasDefaultValue(0L);

		builder.Property(o => o.Deny)
			.HasColumnName("deny")
			.HasDefaultValue(0L);

		builder.Property(o => o.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("NOW()");

		builder.Property(o => o.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("NOW()");

		builder.HasOne<Channel>()
			.WithMany()
			.HasForeignKey(o => o.ChannelId)
			.OnDelete(DeleteBehavior.Cascade);

		// only one overwrite per (channel, target_type, target_id) tuple
		builder.HasIndex(o => new { o.ChannelId, o.TargetType, o.TargetId })
			.IsUnique()
			.HasDatabaseName("unique_overwrite");

		builder.HasIndex(o => o.ChannelId)
			.HasDatabaseName("idx_overwrites_channel");
	}
}
