using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISpace.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddNpcAndShopConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    BannerVisualId = table.Column<long>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shops", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Npcs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MapId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: -1),
                    DayPhase = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: -1),
                    DateStartUtc = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)),
                    DateEndUtc = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValue: new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999)),
                    NpcObjectId = table.Column<long>(type: "INTEGER", nullable: false),
                    ModelId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    X = table.Column<float>(type: "REAL", nullable: false),
                    Y = table.Column<float>(type: "REAL", nullable: false),
                    Z = table.Column<float>(type: "REAL", nullable: false),
                    Rotation = table.Column<int>(type: "INTEGER", nullable: false),
                    ShopId = table.Column<int>(type: "INTEGER", nullable: true),
                    InteractionType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Npcs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Npcs_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ShopItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShopId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    AiPrice = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    NicoPrice = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopItems_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NpcEquipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NpcId = table.Column<int>(type: "INTEGER", nullable: false),
                    SlotIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NpcEquipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NpcEquipment_Npcs_NpcId",
                        column: x => x.NpcId,
                        principalTable: "Npcs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NpcEquipment_NpcId_SlotIndex",
                table: "NpcEquipment",
                columns: new[] { "NpcId", "SlotIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Npcs_MapId_SortOrder",
                table: "Npcs",
                columns: new[] { "MapId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Npcs_NpcObjectId",
                table: "Npcs",
                column: "NpcObjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Npcs_ShopId",
                table: "Npcs",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopItems_ShopId_ItemId",
                table: "ShopItems",
                columns: new[] { "ShopId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopItems_ShopId_SortOrder",
                table: "ShopItems",
                columns: new[] { "ShopId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Shops_Code",
                table: "Shops",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NpcEquipment");

            migrationBuilder.DropTable(
                name: "ShopItems");

            migrationBuilder.DropTable(
                name: "Npcs");

            migrationBuilder.DropTable(
                name: "Shops");
        }
    }
}
