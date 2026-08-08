using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISpace.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCircleMembershipAndProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Circles",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT"
            );

            migrationBuilder.AddColumn<string>(
                name: "Mark",
                table: "Circles",
                type: "TEXT",
                maxLength: 37,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<uint>(
                name: "MarkId",
                table: "Circles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u
            );

            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "Circles",
                type: "TEXT",
                maxLength: 751,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "MessageDate",
                table: "Circles",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Circles",
                type: "TEXT",
                maxLength: 46,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<uint>(
                name: "Status",
                table: "Circles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1u
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Circles",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP"
            );

            migrationBuilder.CreateTable(
                name: "CircleJoinRequests",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CircleId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequesterCharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetCharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<byte>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(
                        type: "TEXT",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircleJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CircleJoinRequests_Characters_RequesterCharacterId",
                        column: x => x.RequesterCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_CircleJoinRequests_Characters_TargetCharacterId",
                        column: x => x.TargetCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_CircleJoinRequests_Circles_CircleId",
                        column: x => x.CircleId,
                        principalTable: "Circles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "CircleMembers",
                columns: table => new
                {
                    CircleId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthLevel = table.Column<uint>(type: "INTEGER", nullable: false),
                    JoinedAt = table.Column<DateTime>(
                        type: "TEXT",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircleMembers", x => new { x.CircleId, x.CharacterId });
                    table.ForeignKey(
                        name: "FK_CircleMembers_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_CircleMembers_Circles_CircleId",
                        column: x => x.CircleId,
                        principalTable: "Circles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Circles_LeaderCharacterId",
                table: "Circles",
                column: "LeaderCharacterId"
            );

            migrationBuilder.CreateIndex(name: "IX_Circles_Name", table: "Circles", column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CircleJoinRequests_CircleId_TargetCharacterId_Status",
                table: "CircleJoinRequests",
                columns: new[] { "CircleId", "TargetCharacterId", "Status" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_CircleJoinRequests_RequesterCharacterId_Status",
                table: "CircleJoinRequests",
                columns: new[] { "RequesterCharacterId", "Status" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_CircleJoinRequests_TargetCharacterId_Status",
                table: "CircleJoinRequests",
                columns: new[] { "TargetCharacterId", "Status" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_CircleMembers_CharacterId",
                table: "CircleMembers",
                column: "CharacterId"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Circles_Characters_LeaderCharacterId",
                table: "Circles",
                column: "LeaderCharacterId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            // Backfill memberships from legacy Characters.CircleId.
            migrationBuilder.Sql(
                """
                INSERT INTO CircleMembers (CircleId, CharacterId, AuthLevel, JoinedAt)
                SELECT c.CircleId, c.Id,
                       CASE WHEN circ.LeaderCharacterId = c.Id THEN 2 ELSE 0 END,
                       CURRENT_TIMESTAMP
                FROM Characters c
                INNER JOIN Circles circ ON circ.Id = c.CircleId
                WHERE c.CircleId IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM CircleMembers m
                      WHERE m.CircleId = c.CircleId AND m.CharacterId = c.Id
                  );
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Circles_Characters_LeaderCharacterId",
                table: "Circles"
            );

            migrationBuilder.DropTable(name: "CircleJoinRequests");

            migrationBuilder.DropTable(name: "CircleMembers");

            migrationBuilder.DropIndex(name: "IX_Circles_LeaderCharacterId", table: "Circles");

            migrationBuilder.DropIndex(name: "IX_Circles_Name", table: "Circles");

            migrationBuilder.DropColumn(name: "Mark", table: "Circles");

            migrationBuilder.DropColumn(name: "MarkId", table: "Circles");

            migrationBuilder.DropColumn(name: "Message", table: "Circles");

            migrationBuilder.DropColumn(name: "MessageDate", table: "Circles");

            migrationBuilder.DropColumn(name: "Name", table: "Circles");

            migrationBuilder.DropColumn(name: "Status", table: "Circles");

            migrationBuilder.DropColumn(name: "UpdatedAt", table: "Circles");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Circles",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP"
            );
        }
    }
}
