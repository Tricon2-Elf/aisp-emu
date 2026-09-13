using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class SeededDramaCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DramaAudio",
                columns: table => new
                {
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DramaAudio", x => new { x.Kind, x.Id });
                    table.ForeignKey(
                        name: "FK_DramaAudio_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "DramaFigureBoxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DramaFigureBoxes", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "DramaFigures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    BoxId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Gender = table.Column<int>(type: "INTEGER", nullable: false),
                    People = table.Column<int>(type: "INTEGER", nullable: false),
                    PackageId = table.Column<int>(type: "INTEGER", nullable: false),
                    Face = table.Column<int>(type: "INTEGER", nullable: false),
                    Hairstyle = table.Column<int>(type: "INTEGER", nullable: false),
                    ModelId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlwaysGranted = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DramaFigures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DramaFigures_DramaFigureBoxes_BoxId",
                        column: x => x.BoxId,
                        principalTable: "DramaFigureBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_DramaFigures_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "DramaFigureEquipment",
                columns: table => new
                {
                    FigureId = table.Column<int>(type: "INTEGER", nullable: false),
                    SlotIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_DramaFigureEquipment",
                        x => new { x.FigureId, x.SlotIndex }
                    );
                    table.ForeignKey(
                        name: "FK_DramaFigureEquipment_DramaFigures_FigureId",
                        column: x => x.FigureId,
                        principalTable: "DramaFigures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_DramaAudio_ItemId",
                table: "DramaAudio",
                column: "ItemId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_DramaFigures_BoxId",
                table: "DramaFigures",
                column: "BoxId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_DramaFigures_ItemId",
                table: "DramaFigures",
                column: "ItemId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DramaAudio");

            migrationBuilder.DropTable(name: "DramaFigureEquipment");

            migrationBuilder.DropTable(name: "DramaFigures");

            migrationBuilder.DropTable(name: "DramaFigureBoxes");
        }
    }
}
