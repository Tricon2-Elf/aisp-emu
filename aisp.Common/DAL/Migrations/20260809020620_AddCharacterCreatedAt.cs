using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite cannot add a column with a non-constant default such as CURRENT_TIMESTAMP.
            // Add it with a constant, backfill existing rows, then rebuild with the desired default.
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: epoch
            );

            migrationBuilder.Sql("UPDATE \"Characters\" SET \"CreatedAt\" = CURRENT_TIMESTAMP;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: false,
                oldDefaultValue: epoch
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CreatedAt", table: "Characters");
        }
    }
}
