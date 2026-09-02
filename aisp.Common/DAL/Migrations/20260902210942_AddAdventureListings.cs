using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddAdventureListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AdventureSalesBalance",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "AdventureListings",
                columns: table => new
                {
                    ScriptId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    AuthorName = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Genre = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 768, nullable: false),
                    Price = table.Column<long>(type: "INTEGER", nullable: false),
                    ContentsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    Official = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ContentSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Pages = table.Column<int>(type: "INTEGER", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    SalesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DownloadCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ListedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DelistedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdventureListings", x => x.ScriptId);
                    table.ForeignKey(
                        name: "FK_AdventureListings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdventureTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Token = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ScriptId = table.Column<long>(type: "INTEGER", nullable: false),
                    Purpose = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdventureTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdventureListingContents",
                columns: table => new
                {
                    ScriptId = table.Column<long>(type: "INTEGER", nullable: false),
                    Script = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Datalist = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdventureListingContents", x => x.ScriptId);
                    table.ForeignKey(
                        name: "FK_AdventureListingContents_AdventureListings_ScriptId",
                        column: x => x.ScriptId,
                        principalTable: "AdventureListings",
                        principalColumn: "ScriptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdventurePurchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ScriptId = table.Column<long>(type: "INTEGER", nullable: false),
                    BuyerUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    BuyerCharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<long>(type: "INTEGER", nullable: false),
                    AuthorShare = table.Column<long>(type: "INTEGER", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    SettledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HiddenFromHistory = table.Column<bool>(type: "INTEGER", nullable: false),
                    HiddenFromDownloads = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdventurePurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdventurePurchases_AdventureListings_ScriptId",
                        column: x => x.ScriptId,
                        principalTable: "AdventureListings",
                        principalColumn: "ScriptId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdventurePurchases_Users_BuyerUserId",
                        column: x => x.BuyerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdventureListings_State_Genre",
                table: "AdventureListings",
                columns: new[] { "State", "Genre" });

            migrationBuilder.CreateIndex(
                name: "IX_AdventureListings_UserId_WorkId",
                table: "AdventureListings",
                columns: new[] { "UserId", "WorkId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdventurePurchases_BuyerUserId_ScriptId",
                table: "AdventurePurchases",
                columns: new[] { "BuyerUserId", "ScriptId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdventurePurchases_ScriptId",
                table: "AdventurePurchases",
                column: "ScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_AdventurePurchases_SettledAt",
                table: "AdventurePurchases",
                column: "SettledAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdventureTickets_ExpiresAt",
                table: "AdventureTickets",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdventureTickets_Token",
                table: "AdventureTickets",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdventureListingContents");

            migrationBuilder.DropTable(
                name: "AdventurePurchases");

            migrationBuilder.DropTable(
                name: "AdventureTickets");

            migrationBuilder.DropTable(
                name: "AdventureListings");

            migrationBuilder.DropColumn(
                name: "AdventureSalesBalance",
                table: "Users");
        }
    }
}
