using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddFriendships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FriendRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequesterCharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetCharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<byte>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FriendRequests_Characters_RequesterCharacterId",
                        column: x => x.RequesterCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FriendRequests_Characters_TargetCharacterId",
                        column: x => x.TargetCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Friendships",
                columns: table => new
                {
                    CharacterIdLow = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterIdHigh = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendships", x => new { x.CharacterIdLow, x.CharacterIdHigh });
                    table.ForeignKey(
                        name: "FK_Friendships_Characters_CharacterIdHigh",
                        column: x => x.CharacterIdHigh,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Friendships_Characters_CharacterIdLow",
                        column: x => x.CharacterIdLow,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequests_RequesterCharacterId_Status",
                table: "FriendRequests",
                columns: new[] { "RequesterCharacterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequests_TargetCharacterId_Status",
                table: "FriendRequests",
                columns: new[] { "TargetCharacterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_CharacterIdHigh",
                table: "Friendships",
                column: "CharacterIdHigh");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FriendRequests");

            migrationBuilder.DropTable(
                name: "Friendships");
        }
    }
}
