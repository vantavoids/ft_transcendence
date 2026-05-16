using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.Persistence.Db.Configurations;

internal sealed class GuildConfig : IEntityTypeConfiguration<GuildEntity>
{
	public void Configure(EntityTypeBuilder<GuildEntity> builder)
	{
		builder.ToTable("guilds");

		builder.HasKey(g => g.Id);
		builder.Property(g => g.Id)
			.HasColumnName("id")
			.ValueGeneratedNever();

		builder.Property(g => g.Name)
			.HasColumnName("name")
			.HasMaxLength(GuildEntity.MaxNameLen)
			.IsRequired();

		builder.Property(g => g.Description)
			.HasColumnName("description")
			.HasColumnType("text");

		builder.Property(g => g.IconUrl)
			.HasColumnName("icon_url")
			.HasMaxLength(GuildEntity.MaxUrlLen);

		builder.Property(g => g.BannerUrl)
			.HasColumnName("banner_url")
			.HasMaxLength(GuildEntity.MaxUrlLen);

		builder.Property(g => g.OwnerId)
			.HasColumnName("owner_id")
			.IsRequired();

		builder.Property(g => g.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("NOW()");

		builder.Property(g => g.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("NOW()");

		builder.HasIndex(g => g.OwnerId)
			.HasDatabaseName("idx_guilds_owner");

		// owned aggregates: cascade delete is handled at the FK level on each child
		builder.HasMany(g => g.Roles)
			.WithOne()
			.HasForeignKey(r => r.GuildId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(g => g.Members)
			.WithOne()
			.HasForeignKey(m => m.GuildId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(g => g.MemberRoles)
			.WithOne()
			.HasForeignKey(mr => mr.GuildId)
			.OnDelete(DeleteBehavior.Cascade);

		// tell EF that the private List<> backing fields are the collections it
		// should populate when materialising the aggregate
		builder.Metadata
			.FindNavigation(nameof(GuildEntity.Roles))!
			.SetField("_roles");
		builder.Metadata
			.FindNavigation(nameof(GuildEntity.Members))!
			.SetField("_members");
		builder.Metadata
			.FindNavigation(nameof(GuildEntity.MemberRoles))!
			.SetField("_memberRoles");
	}
}
