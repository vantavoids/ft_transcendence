using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guild.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixOverwriteGuildIdAndTargetType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "target_type",
                table: "channel_permission_overwrites",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<long>(
                name: "guild_id",
                table: "channel_permission_overwrites",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "idx_overwrites_guild",
                table: "channel_permission_overwrites",
                column: "guild_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_overwrites_guild",
                table: "channel_permission_overwrites");

            migrationBuilder.DropColumn(
                name: "guild_id",
                table: "channel_permission_overwrites");

            migrationBuilder.AlterColumn<int>(
                name: "target_type",
                table: "channel_permission_overwrites",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
