using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Auth.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auth_users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    email_verified = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    oauth_provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    oauth_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    refresh_token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    refresh_token_issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    refresh_token_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    refresh_token_revoked = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_users", x => x.id);
                    table.CheckConstraint("email_or_oauth", "(\n            (email IS NOT NULL AND password_hash IS NOT NULL)\n            OR (oauth_provider IS NOT NULL AND oauth_id IS NOT NULL)\n        )");
                });

            migrationBuilder.CreateIndex(
                name: "idx_users_auth_email",
                table: "auth_users",
                column: "email",
                unique: true,
                filter: "deleted_at IS NULL AND email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_users_auth_oauth",
                table: "auth_users",
                columns: new[] { "oauth_provider", "oauth_id" },
                unique: true,
                filter: "deleted_at IS NULL AND oauth_provider IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_users_auth_refresh_token",
                table: "auth_users",
                column: "refresh_token_hash",
                filter: "deleted_at IS NULL\n                        AND refresh_token_revoked = FALSE\n                        AND refresh_token_hash IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_users");
        }
    }
}
