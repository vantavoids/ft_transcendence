using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace Auth.Persistence.Db;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<AuthUser> AuthUsers => Set<AuthUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuthUser>().ToTable(b =>
            b.HasCheckConstraint("email_or_oauth", @"(
                ([email] IS NOT NULL AND [password_hash] IS NOT NULL)
                OR ([oauth_provider] IS NOT NULL AND [oauth_id] IS NOT NULL)
            )")
        );

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}
