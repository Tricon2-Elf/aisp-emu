using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalisation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "PreferredLanguage",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: (byte)0
            );

            migrationBuilder.AddColumn<int>(
                name: "CatalogCategory",
                table: "Items",
                type: "INTEGER",
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "LocalisedTexts",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Language = table.Column<byte>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalisedTexts", x => new { x.Key, x.Language });
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_LocalisedTexts_Key",
                table: "LocalisedTexts",
                column: "Key"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LocalisedTexts");

            migrationBuilder.DropColumn(name: "PreferredLanguage", table: "Users");

            migrationBuilder.DropColumn(name: "CatalogCategory", table: "Items");
        }
    }
}
