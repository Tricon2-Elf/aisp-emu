using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddAdventureWorks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdventureSheetStock",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "NextAdventureWorkId",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1
            );

            migrationBuilder.CreateTable(
                name: "AdventureWorks",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sheets = table.Column<int>(type: "INTEGER", nullable: false),
                    Uploaded = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(
                        type: "TEXT",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdventureWorks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdventureWorks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AdventureWorks_UserId_WorkId",
                table: "AdventureWorks",
                columns: new[] { "UserId", "WorkId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AdventureWorks");

            migrationBuilder.DropColumn(name: "AdventureSheetStock", table: "Users");

            migrationBuilder.DropColumn(name: "NextAdventureWorkId", table: "Users");
        }
    }
}
