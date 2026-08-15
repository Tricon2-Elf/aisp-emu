using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddRoboPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Robos",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoboId = table.Column<uint>(type: "INTEGER", nullable: false),
                    State = table.Column<uint>(type: "INTEGER", nullable: false),
                    AiScriptId = table.Column<ushort>(type: "INTEGER", nullable: false),
                    ModelId = table.Column<uint>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 37, nullable: false),
                    BloodType = table.Column<uint>(type: "INTEGER", nullable: false),
                    BirthMonth = table.Column<byte>(type: "INTEGER", nullable: false),
                    BirthDay = table.Column<byte>(type: "INTEGER", nullable: false),
                    Gender = table.Column<uint>(type: "INTEGER", nullable: false),
                    Face = table.Column<byte>(type: "INTEGER", nullable: false),
                    Hairstyle = table.Column<uint>(type: "INTEGER", nullable: false),
                    ParameterId = table.Column<uint>(type: "INTEGER", nullable: false),
                    JobId = table.Column<uint>(type: "INTEGER", nullable: false),
                    Level = table.Column<byte>(type: "INTEGER", nullable: false),
                    StatusPoints = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Experience = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ExperienceToNextLevel = table.Column<ulong>(type: "INTEGER", nullable: false),
                    AvailableStatusPoints = table.Column<uint>(type: "INTEGER", nullable: false),
                    UserStatusText = table.Column<string>(
                        type: "TEXT",
                        maxLength: 49,
                        nullable: false
                    ),
                    UserStatusIconId = table.Column<uint>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_Robos", x => new { x.CharacterId, x.RoboId });
                    table.ForeignKey(
                        name: "FK_Robos_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RoboDistributedStatusPoints",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoboId = table.Column<uint>(type: "INTEGER", nullable: false),
                    StatusIndex = table.Column<byte>(type: "INTEGER", nullable: false),
                    Value = table.Column<uint>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_RoboDistributedStatusPoints",
                        x => new
                        {
                            x.CharacterId,
                            x.RoboId,
                            x.StatusIndex,
                        }
                    );
                    table.ForeignKey(
                        name: "FK_RoboDistributedStatusPoints_Robos_CharacterId_RoboId",
                        columns: x => new { x.CharacterId, x.RoboId },
                        principalTable: "Robos",
                        principalColumns: new[] { "CharacterId", "RoboId" },
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RoboEquipment",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoboId = table.Column<uint>(type: "INTEGER", nullable: false),
                    SlotIndex = table.Column<byte>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<uint>(type: "INTEGER", nullable: false),
                    Socket = table.Column<uint>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_RoboEquipment",
                        x => new
                        {
                            x.CharacterId,
                            x.RoboId,
                            x.SlotIndex,
                        }
                    );
                    table.ForeignKey(
                        name: "FK_RoboEquipment_Robos_CharacterId_RoboId",
                        columns: x => new { x.CharacterId, x.RoboId },
                        principalTable: "Robos",
                        principalColumns: new[] { "CharacterId", "RoboId" },
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RoboItemUseEffects",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoboId = table.Column<uint>(type: "INTEGER", nullable: false),
                    SlotIndex = table.Column<byte>(type: "INTEGER", nullable: false),
                    ItemSerialId = table.Column<uint>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<uint>(type: "INTEGER", nullable: false),
                    ItemDefinitionId = table.Column<uint>(type: "INTEGER", nullable: false),
                    EffectType = table.Column<uint>(type: "INTEGER", nullable: false),
                    Parameter0 = table.Column<uint>(type: "INTEGER", nullable: false),
                    Parameter1 = table.Column<uint>(type: "INTEGER", nullable: false),
                    Parameter2 = table.Column<uint>(type: "INTEGER", nullable: false),
                    Parameter3 = table.Column<uint>(type: "INTEGER", nullable: false),
                    Parameter4 = table.Column<uint>(type: "INTEGER", nullable: false),
                    OverwriteExisting = table.Column<byte>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_RoboItemUseEffects",
                        x => new
                        {
                            x.CharacterId,
                            x.RoboId,
                            x.SlotIndex,
                        }
                    );
                    table.ForeignKey(
                        name: "FK_RoboItemUseEffects_Robos_CharacterId_RoboId",
                        columns: x => new { x.CharacterId, x.RoboId },
                        principalTable: "Robos",
                        principalColumns: new[] { "CharacterId", "RoboId" },
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RoboTpsBattleData",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoboId = table.Column<uint>(type: "INTEGER", nullable: false),
                    ActionReferenceX = table.Column<float>(type: "REAL", nullable: false),
                    ActionReferenceY = table.Column<float>(type: "REAL", nullable: false),
                    ActionProfileId = table.Column<uint>(type: "INTEGER", nullable: false),
                    CollisionRadius = table.Column<float>(type: "REAL", nullable: false),
                    ActionVerticalRange = table.Column<float>(type: "REAL", nullable: false),
                    HitPointsCurrent = table.Column<uint>(type: "INTEGER", nullable: false),
                    HitPointsBaseMaximum = table.Column<uint>(type: "INTEGER", nullable: false),
                    HitPointsMaximumBonus = table.Column<uint>(type: "INTEGER", nullable: false),
                    HitPointsMaximumPenalty = table.Column<uint>(type: "INTEGER", nullable: false),
                    CurrentHearts = table.Column<byte>(type: "INTEGER", nullable: false),
                    MaximumHearts = table.Column<byte>(type: "INTEGER", nullable: false),
                    StaminaCurrent = table.Column<float>(type: "REAL", nullable: false),
                    StaminaRecoveryRate = table.Column<float>(type: "REAL", nullable: false),
                    StaminaCostReductionBonus = table.Column<uint>(
                        type: "INTEGER",
                        nullable: false
                    ),
                    StaminaCostReductionPenalty = table.Column<uint>(
                        type: "INTEGER",
                        nullable: false
                    ),
                    TankCurrent = table.Column<uint>(type: "INTEGER", nullable: false),
                    TankBaseMaximum = table.Column<uint>(type: "INTEGER", nullable: false),
                    TankMaximumBonus = table.Column<uint>(type: "INTEGER", nullable: false),
                    TankMaximumPenalty = table.Column<uint>(type: "INTEGER", nullable: false),
                    StatusEffectFlags = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ActionFlags = table.Column<uint>(type: "INTEGER", nullable: false),
                    ActiveSkillId = table.Column<uint>(type: "INTEGER", nullable: false),
                    CosplayId = table.Column<uint>(type: "INTEGER", nullable: false),
                    CosplayLevel = table.Column<byte>(type: "INTEGER", nullable: false),
                    CosplayStatusPoints = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CosplayExperience = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CosplayExperienceToNextLevel = table.Column<ulong>(
                        type: "INTEGER",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoboTpsBattleData", x => new { x.CharacterId, x.RoboId });
                    table.ForeignKey(
                        name: "FK_RoboTpsBattleData_Robos_CharacterId_RoboId",
                        columns: x => new { x.CharacterId, x.RoboId },
                        principalTable: "Robos",
                        principalColumns: new[] { "CharacterId", "RoboId" },
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RoboBattleAbilities",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoboId = table.Column<uint>(type: "INTEGER", nullable: false),
                    AbilitySet = table.Column<byte>(type: "INTEGER", nullable: false),
                    AbilityIndex = table.Column<byte>(type: "INTEGER", nullable: false),
                    Value = table.Column<uint>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_RoboBattleAbilities",
                        x => new
                        {
                            x.CharacterId,
                            x.RoboId,
                            x.AbilitySet,
                            x.AbilityIndex,
                        }
                    );
                    table.ForeignKey(
                        name: "FK_RoboBattleAbilities_RoboTpsBattleData_CharacterId_RoboId",
                        columns: x => new { x.CharacterId, x.RoboId },
                        principalTable: "RoboTpsBattleData",
                        principalColumns: new[] { "CharacterId", "RoboId" },
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RoboBattleAbilities");

            migrationBuilder.DropTable(name: "RoboDistributedStatusPoints");

            migrationBuilder.DropTable(name: "RoboEquipment");

            migrationBuilder.DropTable(name: "RoboItemUseEffects");

            migrationBuilder.DropTable(name: "RoboTpsBattleData");

            migrationBuilder.DropTable(name: "Robos");
        }
    }
}
