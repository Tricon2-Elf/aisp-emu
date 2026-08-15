using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RenameUserCurrencyColumnsToAiNico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(name: "NpsPoints", table: "Users", newName: "AiPoints");

            migrationBuilder.RenameColumn(
                name: "NiconicoPoints",
                table: "Users",
                newName: "NicoPoints"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(name: "AiPoints", table: "Users", newName: "NpsPoints");

            migrationBuilder.RenameColumn(
                name: "NicoPoints",
                table: "Users",
                newName: "NiconicoPoints"
            );
        }
    }
}
