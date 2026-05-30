using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guild.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropVoiceChannelType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // guild.sql may have pre-created this type with stale values; drop it so
            // AlterDatabase below can (re)create it with the correct labels.
            migrationBuilder.Sql("DROP TYPE IF EXISTS channel_type;");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:channel_type", "text,announcement");

            migrationBuilder.Sql("DELETE FROM channels WHERE type = 1;");

            migrationBuilder.Sql("""
                ALTER TABLE channels
                    ALTER COLUMN type TYPE channel_type
                    USING CASE type
                        WHEN 0 THEN 'text'::channel_type
                        WHEN 2 THEN 'announcement'::channel_type
                    END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE channels
                    ALTER COLUMN type TYPE integer
                    USING CASE type
                        WHEN 'text'::channel_type    THEN 0
                        WHEN 'announcement'::channel_type THEN 2
                    END;
                """);

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:channel_type", "text,announcement");
        }
    }
}
