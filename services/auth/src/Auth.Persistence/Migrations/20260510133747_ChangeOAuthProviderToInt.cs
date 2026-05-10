using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeOAuthProviderToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE auth_users ALTER COLUMN oauth_provider TYPE integer USING oauth_provider::integer;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE auth_users ALTER COLUMN oauth_provider TYPE character varying(32) USING oauth_provider::varchar;"
            );
        }
    }
}
