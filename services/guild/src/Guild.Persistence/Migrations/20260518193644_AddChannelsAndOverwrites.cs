using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guild.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class AddChannelsAndOverwrites : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "channels",
				columns: table => new
				{
					id = table.Column<long>(type: "bigint", nullable: false),
					guild_id = table.Column<long>(type: "bigint", nullable: false),
					category_id = table.Column<long>(type: "bigint", nullable: true),
					name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
					topic = table.Column<string>(type: "text", nullable: true),
					type = table.Column<int>(type: "integer", nullable: false),
					position = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
					created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
					updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_channels", x => x.id);
					table.ForeignKey(
						name: "FK_channels_channel_categories_category_id",
						column: x => x.category_id,
						principalTable: "channel_categories",
						principalColumn: "id",
						onDelete: ReferentialAction.SetNull);
					table.ForeignKey(
						name: "FK_channels_guilds_guild_id",
						column: x => x.guild_id,
						principalTable: "guilds",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "channel_permission_overwrites",
				columns: table => new
				{
					id = table.Column<long>(type: "bigint", nullable: false),
					channel_id = table.Column<long>(type: "bigint", nullable: false),
					target_type = table.Column<int>(type: "integer", nullable: false),
					target_id = table.Column<long>(type: "bigint", nullable: false),
					allow = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
					deny = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
					created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
					updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_channel_permission_overwrites", x => x.id);
					table.ForeignKey(
						name: "FK_channel_permission_overwrites_channels_channel_id",
						column: x => x.channel_id,
						principalTable: "channels",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "idx_overwrites_channel",
				table: "channel_permission_overwrites",
				column: "channel_id");

			migrationBuilder.CreateIndex(
				name: "unique_overwrite",
				table: "channel_permission_overwrites",
				columns: new[] { "channel_id", "target_type", "target_id" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "idx_channels_guild_category_position",
				table: "channels",
				columns: new[] { "guild_id", "category_id", "position" });

			migrationBuilder.CreateIndex(
				name: "IX_channels_category_id",
				table: "channels",
				column: "category_id");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "channel_permission_overwrites");

			migrationBuilder.DropTable(
				name: "channels");
		}
	}
}
