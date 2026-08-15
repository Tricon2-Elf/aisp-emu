using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddNicotvFurnitureState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nicotvs",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoomId = table.Column<int>(type: "INTEGER", nullable: false),
                    FurnitureId = table.Column<uint>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<uint>(type: "INTEGER", nullable: false),
                    MovieId = table.Column<string>(
                        type: "TEXT",
                        maxLength: 96,
                        nullable: false,
                        defaultValue: ""
                    ),
                    PlaybackState = table.Column<uint>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: 0u
                    ),
                    CommentVisibility = table.Column<uint>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: 0u
                    ),
                    UpdatedAt = table.Column<DateTime>(
                        type: "TEXT",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nicotvs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nicotvs_MyRoomFurniture_RoomId_FurnitureId",
                        columns: x => new { x.RoomId, x.FurnitureId },
                        principalTable: "MyRoomFurniture",
                        principalColumns: new[] { "RoomId", "FurnitureId" },
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Nicotvs_RoomId_FurnitureId",
                table: "Nicotvs",
                columns: new[] { "RoomId", "FurnitureId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Nicotvs");
        }
    }
}
