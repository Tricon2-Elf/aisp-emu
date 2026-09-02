using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddReportTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportTickets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReporterUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReporterUsername = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ReporterCharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReporterCharacterName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    MapId = table.Column<uint>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<int>(type: "INTEGER", nullable: false),
                    MapName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Status = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportTicketChatMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReportTicketId = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Rejected = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTicketChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportTicketChatMessages_ReportTickets_ReportTicketId",
                        column: x => x.ReportTicketId,
                        principalTable: "ReportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportTicketPlayers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReportTicketId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTicketPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportTicketPlayers_ReportTickets_ReportTicketId",
                        column: x => x.ReportTicketId,
                        principalTable: "ReportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_MapId_ChannelId_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "MapId", "ChannelId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportTicketChatMessages_ReportTicketId",
                table: "ReportTicketChatMessages",
                column: "ReportTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportTicketChatMessages_ReportTicketId_CreatedAt",
                table: "ReportTicketChatMessages",
                columns: new[] { "ReportTicketId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportTicketPlayers_ReportTicketId",
                table: "ReportTicketPlayers",
                column: "ReportTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportTickets_CreatedAt",
                table: "ReportTickets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReportTickets_Status_CreatedAt",
                table: "ReportTickets",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportTicketChatMessages");

            migrationBuilder.DropTable(
                name: "ReportTicketPlayers");

            migrationBuilder.DropTable(
                name: "ReportTickets");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_MapId_ChannelId_CreatedAt",
                table: "ChatMessages");
        }
    }
}
