using Guild.Domain.Guild;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guild.Persistence.Db.Configurations;

internal sealed class RoleConfig : IEntityTypeConfiguration<Role>
{
	public void Configure(EntityTypeBuilder<Role> builder)
	{
		builder.ToTable("roles");

		builder.HasKey(r => r.Id);
		builder.Property(r => r.Id)
			.HasColumnName("id")
			.ValueGeneratedNever();

		builder.Property(r => r.GuildId)
			.HasColumnName("guild_id")
			.IsRequired();

		builder.Property(r => r.Name)
			.HasColumnName("name")
			.HasMaxLength(Role.MaxNameLen)
			.IsRequired();

		builder.Property(r => r.Color)
			.HasColumnName("color")
			.HasMaxLength(Role.MaxColorLen);

		builder.Property(r => r.Permissions)
			.HasColumnName("permissions")
			.HasDefaultValue(0L);

		builder.Property(r => r.Position)
			.HasColumnName("position")
			.HasDefaultValue(0);

		// NOTE: we deliberately do NOT call HasDefaultValue(false) for bool
		// properties - see commit f2d10a6 (poor auth had a bug with owned-bool
		// defaults). booleans are set explicitly in code :)
		builder.Property(r => r.IsDefault)
			.HasColumnName("is_default");
		builder.Property(r => r.IsHoisted)
			.HasColumnName("is_hoisted");
		builder.Property(r => r.IsMentionable)
			.HasColumnName("is_mentionable");

		builder.Property(r => r.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("NOW()");
		builder.Property(r => r.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("NOW()");

		// composite alternate key so MemberRole can FK to (guild_id, id)
		builder.HasAlternateKey(r => new { r.GuildId, r.Id });

		// unique role names per guild
		builder.HasIndex(r => new { r.GuildId, r.Name })
			.IsUnique()
			.HasDatabaseName("unique_role_name_per_guild");

		// position-ordered lookup. DESC matches docs/schema/guild.sql
		builder.HasIndex(r => new { r.GuildId, r.Position })
			.IsDescending(false, true)
			.HasDatabaseName("idx_roles_guild");

		// fast @everyone lookup
		builder.HasIndex(r => r.GuildId)
			.HasFilter("is_default = TRUE")
			.HasDatabaseName("idx_roles_guild_default");

		// exactly one @everyone role per guild
		builder.HasIndex(r => r.GuildId)
			.IsUnique()
			.HasFilter("is_default = TRUE")
			.HasDatabaseName("idx_one_default_role_per_guild");

		// unique (guild_id, position) is enforced by the raw-SQL constraint
		// `unique_role_position UNIQUE (...) DEFERRABLE INITIALLY DEFERRED`
		// added in the AddRolePositionUniqueConstraint migration. it is
		// intentionally NOT modeled here: EF Core cannot express DEFERRABLE, and
		// the deferred-to-COMMIT check is what lets Guild.ReorderRoles rewrite
		// every position in a single transaction (issue #213). leaving it out of
		// the model keeps the snapshot from trying to own / re-create it
	}
}
