using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guild.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddXminConcurrencyTokens : Migration
    {
        // This migration intentionally has NO DDL.
        //
        // The Guild/Role/GuildMember/GuildBan/GuildInvite entities now map a uint
        // shadow property to Postgres' `xmin` system column as an optimistic-
        // concurrency token (see XminConcurrency + the entity configs). `xmin`
        // already exists on every table as a system column, so it must NOT be
        // created. Npgsql removed UseXminAsConcurrencyToken() in 7.0+ and its
        // IsRowVersion() replacement scaffolds an AddColumn("xmin", "xid") that
        // PostgreSQL rejects ("column name xmin conflicts with a system column").
        // See npgsql/efcore.pg issues #2558 and #3539 (both closed "not planned").
        //
        // The fix is to keep the model/snapshot mapping (so EF appends
        // `WHERE xmin = @original` to every UPDATE/DELETE and raises
        // DbUpdateConcurrencyException on a lost write) while emptying the
        // generated column DDL here. The model snapshot still records the token,
        // so later migrations diff cleanly and never re-scaffold these columns.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
