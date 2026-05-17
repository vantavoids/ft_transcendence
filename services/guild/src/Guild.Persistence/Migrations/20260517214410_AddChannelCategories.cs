using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guild.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class AddChannelCategories : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "channel_categories",
				columns: table => new
				{
					id = table.Column<long>(type: "bigint", nullable: false),
					guild_id = table.Column<long>(type: "bigint", nullable: false),
					name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
					position = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
					created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
					updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_channel_categories", x => x.id);
					table.ForeignKey(
						name: "FK_channel_categories_guilds_guild_id",
						column: x => x.guild_id,
						principalTable: "guilds",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "idx_categories_guild",
				table: "channel_categories",
				columns: new[] { "guild_id", "position" });
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "channel_categories");
		}
	}
}
