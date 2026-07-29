using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISpace.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddNpcScriptedEventKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScriptedEventKey",
                table: "Npcs",
                type: "TEXT",
                maxLength: 128,
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ScriptedEventKey", table: "Npcs");
        }
    }
}
