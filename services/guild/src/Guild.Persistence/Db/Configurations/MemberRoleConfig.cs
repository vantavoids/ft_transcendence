using Guild.Domain.Guild;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guild.Persistence.Db.Configurations;

internal sealed class MemberRoleConfig : IEntityTypeConfiguration<MemberRole>
{
	public void Configure(EntityTypeBuilder<MemberRole> builder)
	{
		builder.ToTable("member_roles");

		builder.HasKey(mr => new { mr.GuildId, mr.UserId, mr.RoleId });

		builder.Property(mr => mr.GuildId).HasColumnName("guild_id");
		builder.Property(mr => mr.UserId).HasColumnName("user_id");
		builder.Property(mr => mr.RoleId).HasColumnName("role_id");

		builder.Property(mr => mr.AssignedAt)
			.HasColumnName("assigned_at")
			.HasDefaultValueSql("NOW()");

		// composite FK on (guild_id, role_id) -> roles (guild_id, id)
		// this enforces, at the DB level, that a role from guild A can never be
		// assigned to a member of guild B.
		builder.HasOne<Role>()
			.WithMany()
			.HasForeignKey(mr => new { mr.GuildId, mr.RoleId })
			.HasPrincipalKey(r => new { r.GuildId, r.Id })
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(mr => new { mr.UserId, mr.GuildId })
			.HasDatabaseName("idx_member_roles_user");

		builder.HasIndex(mr => mr.RoleId)
			.HasDatabaseName("idx_member_roles_role");
	}
}
