using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISpace.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBanAndTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "BanReason", table: "Users", type: "TEXT", maxLength: 256, nullable: true);

            migrationBuilder.AddColumn<DateTime>(name: "BannedAt", table: "Users", type: "TEXT", nullable: true);

            // SQLite cannot ADD COLUMN with a non-constant DEFAULT (e.g. CURRENT_TIMESTAMP).
            // Use a constant default to avoid AlterColumn, which rebuilds the table and runs PRAGMA foreign_keys
            // outside a transaction. Backfill existing rows; new inserts use User.CreatedAt / EF default.
            migrationBuilder.AddColumn<DateTime>(name: "CreatedAt", table: "Users", type: "TEXT", nullable: false, defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.Sql("UPDATE \"Users\" SET \"CreatedAt\" = CURRENT_TIMESTAMP;");

            migrationBuilder.AddColumn<bool>(name: "IsBanned", table: "Users", type: "INTEGER", nullable: false, defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BanReason", table: "Users");

            migrationBuilder.DropColumn(name: "BannedAt", table: "Users");

            migrationBuilder.DropColumn(name: "CreatedAt", table: "Users");

            migrationBuilder.DropColumn(name: "IsBanned", table: "Users");
        }
    }
}
