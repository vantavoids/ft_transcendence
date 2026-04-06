using Auth.Domain.Entities;
using Auth.Domain.ValueObjects;
using Auth.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Auth.Domain.Enums;

namespace Auth.Persistence.Db;

public sealed class AuthUserConfig : IEntityTypeConfiguration<AuthUser>
{
    public void Configure(EntityTypeBuilder<AuthUser> builder)
    {
        /* Key */
        builder.HasKey(u => u.Id).HasName("id");

        /* Embded Properties */
        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("email")
                .HasMaxLength((int)Email.MaxEmailLen);

            email.Property(e => e.IsVerified)
                .HasColumnName("email_verified")
                .HasDefaultValue(false);
        });
        builder.Property(u => u.PasswordHash)
                .HasColumnName("password_hash");

        builder.OwnsOne(u => u.OAuthIdentity, oauth =>
        {
            oauth.Property(o => o.Provider)
                .HasColumnName("oauth_provider")
                .HasMaxLength(32)
                .HasConversion(
                    provider => OAuthProviderRegistry.ToValue(provider),
                    str => OAuthProviderRegistry.FromValue(str) ?? OAuthProvider.Unknown
                );

            oauth.Property(o => o.Id)
                .HasColumnName("oauth_id")
                .HasMaxLength((int)OAuthIdentity.MaxIdLen);
        });

        builder.OwnsOne(u => u.RefreshToken, token =>
        {
            token.Property(t => t.Hash)
                .HasColumnName("refresh_token_hash")
                .HasMaxLength((int)StoredRefreshToken.MaxHashLen);

            token.Property(t => t.IssuedAt)
                .HasColumnName("refresh_token_issued_at");

            token.Property(t => t.ExpiresAt)
                .HasColumnName("refresh_token_expires_at");

            token.Property(t => t.Revoked)
                .HasColumnName("refresh_token_revoked")
                .HasDefaultValue(false);
        });

        /* Properties */
        builder.Property(u => u.DeletedAt)
                .HasColumnName("deleted_at");

        builder.Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

        builder.Property(u => u.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("NOW()");

        /* Index */
        builder.OwnsOne(u => u.Email).HasIndex(e => e.Value)
                .IsUnique()
                .HasFilter("[deleted_at] IS NULL AND [email] IS NOT NULL")
                .HasDatabaseName("idx_users_auth_email");

        builder.OwnsOne(u => u.OAuthIdentity).HasIndex(o => new { o.Provider, o.Id })
                .IsUnique()
                .HasFilter("[deleted_at] IS NULL AND [oauth_provider] IS NOT NULL")
                .HasDatabaseName("idx_users_auth_oauth");

        builder.OwnsOne(u => u.RefreshToken).HasIndex(r => r.Hash)
                .HasFilter(@"[deleted_at] IS NULL
                        AND [refresh_token_revoked] = FALSE
                        AND [refresh_token_hash IS NOT NULL]")
                .HasDatabaseName("idx_users_auth_refresh_token");
    }
}
