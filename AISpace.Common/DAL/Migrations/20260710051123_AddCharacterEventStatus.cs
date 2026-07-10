using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISpace.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterEventStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterEventStatuses",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    EventKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterEventStatuses", x => new { x.CharacterId, x.EventKey });
                    table.ForeignKey(name: "FK_CharacterEventStatuses_Characters_CharacterId", column: x => x.CharacterId, principalTable: "Characters", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                }
            );

            migrationBuilder.CreateIndex(name: "IX_CharacterEventStatuses_CharacterId", table: "CharacterEventStatuses", column: "CharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CharacterEventStatuses");
        }
    }
}
