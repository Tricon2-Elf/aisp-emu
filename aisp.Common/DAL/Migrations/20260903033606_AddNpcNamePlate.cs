using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddNpcNamePlate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "JobId",
                table: "Robos",
                newName: "NamePlate");

            migrationBuilder.AddColumn<uint>(
                name: "NamePlate",
                table: "Npcs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 4u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NamePlate",
                table: "Npcs");

            migrationBuilder.RenameColumn(
                name: "NamePlate",
                table: "Robos",
                newName: "JobId");
        }
    }
}
