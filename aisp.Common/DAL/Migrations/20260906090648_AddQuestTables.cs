using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 193, nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", maxLength: 37, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 769, nullable: false),
                    LocationName = table.Column<string>(
                        type: "TEXT",
                        maxLength: 97,
                        nullable: false
                    ),
                    DefaultChapter = table.Column<ushort>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: (ushort)1
                    ),
                    DefaultRestSec = table.Column<uint>(type: "INTEGER", nullable: false),
                    DefaultTargetName = table.Column<string>(
                        type: "TEXT",
                        maxLength: 37,
                        nullable: false
                    ),
                    DefaultTargetRequired = table.Column<ushort>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: (ushort)1
                    ),
                    AutoStartOnConnect = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quests", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "CharacterQuestHistories",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestId = table.Column<int>(type: "INTEGER", nullable: false),
                    Chapter = table.Column<ushort>(type: "INTEGER", nullable: false),
                    Result = table.Column<byte>(type: "INTEGER", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_CharacterQuestHistories",
                        x => new { x.CharacterId, x.QuestId }
                    );
                    table.ForeignKey(
                        name: "FK_CharacterQuestHistories_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_CharacterQuestHistories_Quests_QuestId",
                        column: x => x.QuestId,
                        principalTable: "Quests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "CharacterQuestWorks",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestId = table.Column<int>(type: "INTEGER", nullable: false),
                    Chapter = table.Column<ushort>(type: "INTEGER", nullable: false),
                    RestSec = table.Column<uint>(type: "INTEGER", nullable: false),
                    TargetNow = table.Column<ushort>(type: "INTEGER", nullable: false),
                    TargetRequired = table.Column<ushort>(type: "INTEGER", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_CharacterQuestWorks",
                        x => new { x.CharacterId, x.QuestId }
                    );
                    table.ForeignKey(
                        name: "FK_CharacterQuestWorks_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_CharacterQuestWorks_Quests_QuestId",
                        column: x => x.QuestId,
                        principalTable: "Quests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_CharacterQuestHistories_CharacterId",
                table: "CharacterQuestHistories",
                column: "CharacterId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CharacterQuestHistories_QuestId",
                table: "CharacterQuestHistories",
                column: "QuestId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CharacterQuestWorks_CharacterId",
                table: "CharacterQuestWorks",
                column: "CharacterId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CharacterQuestWorks_QuestId",
                table: "CharacterQuestWorks",
                column: "QuestId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CharacterQuestHistories");

            migrationBuilder.DropTable(name: "CharacterQuestWorks");

            migrationBuilder.DropTable(name: "Quests");
        }
    }
}
