using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddMailboxPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MailMessages",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SenderCharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    SenderName = table.Column<string>(type: "TEXT", maxLength: 37, nullable: false),
                    DestinationType = table.Column<byte>(type: "INTEGER", nullable: false),
                    DestinationId = table.Column<int>(type: "INTEGER", nullable: false),
                    DestinationName = table.Column<string>(
                        type: "TEXT",
                        maxLength: 46,
                        nullable: false
                    ),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 91, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 751, nullable: false),
                    SenderDeleted = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: false
                    ),
                    CreatedAtUtc = table.Column<DateTime>(
                        type: "TEXT",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailMessages", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "MailRecipients",
                columns: table => new
                {
                    MailMessageId = table.Column<long>(type: "INTEGER", nullable: false),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    InboxType = table.Column<byte>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: (byte)0
                    ),
                    IsRead = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: false
                    ),
                    IsProtected = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: false
                    ),
                    IsDeleted = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_MailRecipients",
                        x => new { x.MailMessageId, x.CharacterId }
                    );
                    table.ForeignKey(
                        name: "FK_MailRecipients_MailMessages_MailMessageId",
                        column: x => x.MailMessageId,
                        principalTable: "MailMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_MailMessages_SenderCharacterId_CreatedAtUtc",
                table: "MailMessages",
                columns: new[] { "SenderCharacterId", "CreatedAtUtc" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_MailRecipients_CharacterId_IsDeleted_MailMessageId",
                table: "MailRecipients",
                columns: new[] { "CharacterId", "IsDeleted", "MailMessageId" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MailRecipients");

            migrationBuilder.DropTable(name: "MailMessages");
        }
    }
}
