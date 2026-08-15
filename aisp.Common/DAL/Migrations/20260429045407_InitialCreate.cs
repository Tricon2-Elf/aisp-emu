using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelNum = table.Column<int>(type: "INTEGER", nullable: false),
                    Port = table.Column<ushort>(type: "INTEGER", nullable: false),
                    IP = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CurrentUsers = table.Column<uint>(type: "INTEGER", nullable: false),
                    MaxUsers = table.Column<uint>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: 1000u
                    ),
                    MapId = table.Column<uint>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: 10990100u
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Circles",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeaderCharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Circles", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "MapLinks",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceMapId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionX = table.Column<float>(type: "REAL", nullable: false),
                    PositionY = table.Column<float>(type: "REAL", nullable: false),
                    PositionZ = table.Column<float>(type: "REAL", nullable: false),
                    Yaw = table.Column<byte>(type: "INTEGER", nullable: false),
                    Length = table.Column<float>(type: "REAL", nullable: false),
                    Depth = table.Column<float>(type: "REAL", nullable: false),
                    DestinationMapIds = table.Column<string>(
                        type: "TEXT",
                        maxLength: 256,
                        nullable: false
                    ),
                    Behavior = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapLinks", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    MapId = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    SpawnX = table.Column<float>(type: "REAL", nullable: false),
                    SpawnY = table.Column<float>(type: "REAL", nullable: false),
                    SpawnZ = table.Column<float>(type: "REAL", nullable: false),
                    SpawnRotation = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.MapId);
                }
            );

            migrationBuilder.CreateTable(
                name: "PendingMapTransfers",
                columns: table => new
                {
                    UserId = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MapId = table.Column<uint>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<int>(type: "INTEGER", nullable: false),
                    X = table.Column<float>(type: "REAL", nullable: false),
                    Y = table.Column<float>(type: "REAL", nullable: false),
                    Z = table.Column<float>(type: "REAL", nullable: false),
                    Rotation = table.Column<sbyte>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingMapTransfers", x => x.UserId);
                }
            );

            migrationBuilder.CreateTable(
                name: "SessionPresences",
                columns: table => new
                {
                    ConnectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerType = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterId = table.Column<uint>(type: "INTEGER", nullable: false),
                    MapId = table.Column<uint>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<int>(type: "INTEGER", nullable: false),
                    X = table.Column<float>(type: "REAL", nullable: false),
                    Y = table.Column<float>(type: "REAL", nullable: false),
                    Z = table.Column<float>(type: "REAL", nullable: false),
                    Rotation = table.Column<sbyte>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionPresences", x => x.ConnectionId);
                }
            );

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(
                        type: "TEXT",
                        maxLength: 512,
                        nullable: false
                    ),
                    NpsPoints = table.Column<long>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: 0L
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Worlds",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    Port = table.Column<ushort>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Worlds", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ModelId = table.Column<uint>(type: "INTEGER", nullable: false),
                    BloodType = table.Column<uint>(type: "INTEGER", nullable: false),
                    Birthdate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Gender = table.Column<int>(type: "INTEGER", nullable: false),
                    FaceType = table.Column<uint>(type: "INTEGER", nullable: false),
                    Hairstyle = table.Column<uint>(type: "INTEGER", nullable: false),
                    Like1 = table.Column<string>(type: "TEXT", nullable: false),
                    Like2 = table.Column<string>(type: "TEXT", nullable: false),
                    Like3 = table.Column<string>(type: "TEXT", nullable: false),
                    LikeDesc1 = table.Column<string>(type: "TEXT", nullable: false),
                    LikeDesc2 = table.Column<string>(type: "TEXT", nullable: false),
                    LikeDesc3 = table.Column<string>(type: "TEXT", nullable: false),
                    AvatarDesc = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CircleId = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentMapId = table.Column<uint>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_Circles_CircleId",
                        column: x => x.CircleId,
                        principalTable: "Circles",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Characters_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OTP = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "CharacterEquipment",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    SlotIndex = table.Column<byte>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_CharacterEquipment",
                        x => new { x.CharacterId, x.SlotIndex }
                    );
                    table.ForeignKey(
                        name: "FK_CharacterEquipment_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_CharacterEquipment_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "CharacterInventory",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterInventory", x => new { x.CharacterId, x.ItemId });
                    table.ForeignKey(
                        name: "FK_CharacterInventory_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_CharacterInventory_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEquipment_ItemId",
                table: "CharacterEquipment",
                column: "ItemId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CharacterInventory_ItemId",
                table: "CharacterInventory",
                column: "ItemId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Characters_CircleId",
                table: "Characters",
                column: "CircleId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Name",
                table: "Characters",
                column: "Name",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId",
                table: "Characters",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_MapLinks_SourceMapId_ChannelId_SortOrder",
                table: "MapLinks",
                columns: new[] { "SourceMapId", "ChannelId", "SortOrder" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_PendingMapTransfers_ExpiresAtUtc",
                table: "PendingMapTransfers",
                column: "ExpiresAtUtc"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SessionPresences_ServerType_CharacterId",
                table: "SessionPresences",
                columns: new[] { "ServerType", "CharacterId" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_SessionPresences_ServerType_MapId_ChannelId",
                table: "SessionPresences",
                columns: new[] { "ServerType", "MapId", "ChannelId" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_SessionPresences_ServerType_UserId",
                table: "SessionPresences",
                columns: new[] { "ServerType", "UserId" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_SessionPresences_UpdatedAtUtc",
                table: "SessionPresences",
                column: "UpdatedAtUtc"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId_OTP",
                table: "UserSessions",
                columns: new[] { "UserId", "OTP" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Channels");

            migrationBuilder.DropTable(name: "CharacterEquipment");

            migrationBuilder.DropTable(name: "CharacterInventory");

            migrationBuilder.DropTable(name: "MapLinks");

            migrationBuilder.DropTable(name: "Maps");

            migrationBuilder.DropTable(name: "PendingMapTransfers");

            migrationBuilder.DropTable(name: "SessionPresences");

            migrationBuilder.DropTable(name: "UserSessions");

            migrationBuilder.DropTable(name: "Worlds");

            migrationBuilder.DropTable(name: "Characters");

            migrationBuilder.DropTable(name: "Items");

            migrationBuilder.DropTable(name: "Circles");

            migrationBuilder.DropTable(name: "Users");
        }
    }
}
