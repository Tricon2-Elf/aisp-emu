using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<byte>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    DistId = table.Column<uint>(type: "INTEGER", nullable: false),
                    BalloonId = table.Column<uint>(type: "INTEGER", nullable: false),
                    CircleId = table.Column<int>(type: "INTEGER", nullable: true),
                    MapId = table.Column<uint>(type: "INTEGER", nullable: true),
                    ChannelId = table.Column<int>(type: "INTEGER", nullable: true),
                    Rejected = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_CharacterId_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "CharacterId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_CircleId_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "CircleId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_CreatedAt",
                table: "ChatMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_Kind_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "Kind", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_UserId_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_MapId_ChannelId_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "MapId", "ChannelId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");
        }
    }
}
