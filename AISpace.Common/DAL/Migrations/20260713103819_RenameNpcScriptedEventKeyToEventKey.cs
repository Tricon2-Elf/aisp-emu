using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISpace.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RenameNpcScriptedEventKeyToEventKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ScriptedEventKey",
                table: "Npcs",
                newName: "EventKey");

            migrationBuilder.AddColumn<int>(
                name: "EventKind",
                table: "Npcs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE Npcs
                SET EventKind = 1
                WHERE EventKey IS NOT NULL AND EventKey != '';
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventKind",
                table: "Npcs");

            migrationBuilder.RenameColumn(
                name: "EventKey",
                table: "Npcs",
                newName: "ScriptedEventKey");
        }
    }
}
