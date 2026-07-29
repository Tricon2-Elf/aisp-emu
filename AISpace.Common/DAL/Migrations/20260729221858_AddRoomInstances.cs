using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISpace.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomInstances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MyRoomFurniture_Characters_CharacterId",
                table: "MyRoomFurniture"
            );

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OwnerCharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(
                        type: "TEXT",
                        maxLength: 45,
                        nullable: false,
                        defaultValue: "My Room"
                    ),
                    Stage = table.Column<byte>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: (byte)0
                    ),
                    Security = table.Column<uint>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: 0u
                    ),
                    IsDefault = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: false
                    ),
                    CreatedAt = table.Column<DateTime>(
                        type: "TEXT",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                    UpdatedAt = table.Column<DateTime>(
                        type: "TEXT",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rooms_Characters_OwnerCharacterId",
                        column: x => x.OwnerCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.Sql(
                """
                INSERT INTO "Rooms" ("Id", "OwnerCharacterId", "Name", "Stage", "Security", "IsDefault", "CreatedAt", "UpdatedAt")
                SELECT "Id", "Id", "MyRoomName", 0, "MyRoomSecurity", 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                FROM "Characters";
                """
            );

            migrationBuilder.DropColumn(name: "MyRoomName", table: "Characters");

            migrationBuilder.DropColumn(name: "MyRoomSecurity", table: "Characters");

            migrationBuilder.RenameColumn(
                name: "CharacterId",
                table: "MyRoomFurniture",
                newName: "RoomId"
            );

            migrationBuilder.AddColumn<uint>(
                name: "MyRoomId",
                table: "PendingMapTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u
            );

            migrationBuilder.AddColumn<int>(
                name: "CurrentRoomId",
                table: "Characters",
                type: "INTEGER",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Characters_CurrentRoomId",
                table: "Characters",
                column: "CurrentRoomId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_OwnerCharacterId_IsDefault",
                table: "Rooms",
                columns: new[] { "OwnerCharacterId", "IsDefault" }
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Rooms_CurrentRoomId",
                table: "Characters",
                column: "CurrentRoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull
            );

            migrationBuilder.AddForeignKey(
                name: "FK_MyRoomFurniture_Rooms_RoomId",
                table: "MyRoomFurniture",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Rooms_CurrentRoomId",
                table: "Characters"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_MyRoomFurniture_Rooms_RoomId",
                table: "MyRoomFurniture"
            );

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

            migrationBuilder.Sql(
                """
                UPDATE "Characters"
                SET "MyRoomName" = COALESCE(
                        (SELECT "Name" FROM "Rooms" WHERE "OwnerCharacterId" = "Characters"."Id" ORDER BY "IsDefault" DESC, "Id" LIMIT 1),
                        'My Room'
                    ),
                    "MyRoomSecurity" = COALESCE(
                        (SELECT "Security" FROM "Rooms" WHERE "OwnerCharacterId" = "Characters"."Id" ORDER BY "IsDefault" DESC, "Id" LIMIT 1),
                        0
                    );
                """
            );

            migrationBuilder.DropIndex(name: "IX_Characters_CurrentRoomId", table: "Characters");

            migrationBuilder.DropColumn(name: "CurrentRoomId", table: "Characters");

            migrationBuilder.DropColumn(name: "MyRoomId", table: "PendingMapTransfers");

            migrationBuilder.RenameColumn(
                name: "RoomId",
                table: "MyRoomFurniture",
                newName: "CharacterId"
            );

            migrationBuilder.DropTable(name: "Rooms");

            migrationBuilder.AddForeignKey(
                name: "FK_MyRoomFurniture_Characters_CharacterId",
                table: "MyRoomFurniture",
                column: "CharacterId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
