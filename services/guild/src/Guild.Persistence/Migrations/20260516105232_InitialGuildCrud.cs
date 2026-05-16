using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guild.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class InitialGuildCrud : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "guilds",
				columns: table => new
				{
					id = table.Column<long>(type: "bigint", nullable: false),
					name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
					description = table.Column<string>(type: "text", nullable: true),
					icon_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
					banner_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
					owner_id = table.Column<long>(type: "bigint", nullable: false),
					created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
					updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_guilds", x => x.id);
				});

			migrationBuilder.CreateTable(
				name: "guild_members",
				columns: table => new
				{
					guild_id = table.Column<long>(type: "bigint", nullable: false),
					user_id = table.Column<long>(type: "bigint", nullable: false),
					nickname = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
					joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_guild_members", x => new { x.guild_id, x.user_id });
					table.ForeignKey(
						name: "FK_guild_members_guilds_guild_id",
						column: x => x.guild_id,
						principalTable: "guilds",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "roles",
				columns: table => new
				{
					id = table.Column<long>(type: "bigint", nullable: false),
					guild_id = table.Column<long>(type: "bigint", nullable: false),
					name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
					color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
					permissions = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
					position = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
					is_default = table.Column<bool>(type: "boolean", nullable: false),
					is_hoisted = table.Column<bool>(type: "boolean", nullable: false),
					is_mentionable = table.Column<bool>(type: "boolean", nullable: false),
					created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
					updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_roles", x => x.id);
					table.UniqueConstraint("AK_roles_guild_id_id", x => new { x.guild_id, x.id });
					table.ForeignKey(
						name: "FK_roles_guilds_guild_id",
						column: x => x.guild_id,
						principalTable: "guilds",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "member_roles",
				columns: table => new
				{
					guild_id = table.Column<long>(type: "bigint", nullable: false),
					user_id = table.Column<long>(type: "bigint", nullable: false),
					role_id = table.Column<long>(type: "bigint", nullable: false),
					assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_member_roles", x => new { x.guild_id, x.user_id, x.role_id });
					table.ForeignKey(
						name: "FK_member_roles_guilds_guild_id",
						column: x => x.guild_id,
						principalTable: "guilds",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_member_roles_roles_guild_id_role_id",
						columns: x => new { x.guild_id, x.role_id },
						principalTable: "roles",
						principalColumns: new[] { "guild_id", "id" },
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "idx_guild_members_user",
				table: "guild_members",
				column: "user_id");

			migrationBuilder.CreateIndex(
				name: "idx_guilds_owner",
				table: "guilds",
				column: "owner_id");

			migrationBuilder.CreateIndex(
				name: "idx_member_roles_role",
				table: "member_roles",
				column: "role_id");

			migrationBuilder.CreateIndex(
				name: "idx_member_roles_user",
				table: "member_roles",
				columns: new[] { "user_id", "guild_id" });

			migrationBuilder.CreateIndex(
				name: "IX_member_roles_guild_id_role_id",
				table: "member_roles",
				columns: new[] { "guild_id", "role_id" });

			migrationBuilder.CreateIndex(
				name: "idx_one_default_role_per_guild",
				table: "roles",
				column: "guild_id",
				unique: true,
				filter: "is_default = TRUE");

			migrationBuilder.CreateIndex(
				name: "idx_roles_guild",
				table: "roles",
				columns: new[] { "guild_id", "position" },
				descending: new[] { false, true });

			migrationBuilder.CreateIndex(
				name: "unique_role_name_per_guild",
				table: "roles",
				columns: new[] { "guild_id", "name" },
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "guild_members");

			migrationBuilder.DropTable(
				name: "member_roles");

			migrationBuilder.DropTable(
				name: "roles");

			migrationBuilder.DropTable(
				name: "guilds");
		}
	}
}
