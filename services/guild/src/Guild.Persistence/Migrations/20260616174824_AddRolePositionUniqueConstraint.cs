using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guild.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRolePositionUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // unique role position per guild. DEFERRABLE INITIALLY DEFERRED so a
            // bulk reorder (Guild.ReorderRoles) can rewrite every role's position
            // inside one transaction and only have uniqueness checked at COMMIT.
            // raw SQL because EF Core cannot model DEFERRABLE constraints (see
            // RoleConfig). no backfill needed: seeded guilds only hold
            // @everyone (0) + Administrator (1), so no existing duplicates.
            migrationBuilder.Sql(
                "ALTER TABLE roles ADD CONSTRAINT unique_role_position " +
                "UNIQUE (guild_id, position) DEFERRABLE INITIALLY DEFERRED;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE roles DROP CONSTRAINT unique_role_position;");
        }
    }
}
