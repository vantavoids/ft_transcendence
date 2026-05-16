using Guild.Domain.Guild;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guild.Persistence.Db.Configurations;

internal sealed class GuildMemberConfig : IEntityTypeConfiguration<GuildMember>
{
	public void Configure(EntityTypeBuilder<GuildMember> builder)
	{
		builder.ToTable("guild_members");

		builder.HasKey(m => new { m.GuildId, m.UserId });

		builder.Property(m => m.GuildId).HasColumnName("guild_id");
		builder.Property(m => m.UserId).HasColumnName("user_id");

		builder.Property(m => m.Nickname)
			.HasColumnName("nickname")
			.HasMaxLength(GuildMember.MaxNicknameLen);

		builder.Property(m => m.JoinedAt)
			.HasColumnName("joined_at")
			.HasDefaultValueSql("NOW()");

		builder.HasIndex(m => m.UserId)
			.HasDatabaseName("idx_guild_members_user");
	}
}
