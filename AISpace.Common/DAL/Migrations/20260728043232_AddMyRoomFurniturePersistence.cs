using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISpace.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddMyRoomFurniturePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MyRoomName",
                table: "Characters",
                type: "TEXT",
                maxLength: 45,
                nullable: false,
                defaultValue: "My Room"
            );

            migrationBuilder.AddColumn<uint>(
                name: "MyRoomSecurity",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u
            );

            migrationBuilder.CreateTable(
                name: "Furniture",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<uint>(type: "INTEGER", nullable: false),
                    PlacementFlags = table.Column<uint>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Furniture", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_Furniture_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "MyRoomFurniture",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    FurnitureId = table.Column<uint>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionX = table.Column<float>(type: "REAL", nullable: false),
                    PositionY = table.Column<float>(type: "REAL", nullable: false),
                    PositionZ = table.Column<float>(type: "REAL", nullable: false),
                    DirectionX = table.Column<byte>(type: "INTEGER", nullable: false),
                    DirectionY = table.Column<byte>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_MyRoomFurniture",
                        x => new { x.CharacterId, x.FurnitureId }
                    );
                    table.ForeignKey(
                        name: "FK_MyRoomFurniture_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_MyRoomFurniture_Furniture_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Furniture",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_MyRoomFurniture_ItemId",
                table: "MyRoomFurniture",
                column: "ItemId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MyRoomFurniture");

            migrationBuilder.DropTable(name: "Furniture");

            migrationBuilder.DropColumn(name: "MyRoomName", table: "Characters");

            migrationBuilder.DropColumn(name: "MyRoomSecurity", table: "Characters");
        }
    }
}
