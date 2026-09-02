using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.Tests;

public class RoboRepositoryTests
{
    [Fact]
    public async Task Upsert_and_get_all_preserve_persistent_robo_data_across_contexts()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 7, TestContext.Current.CancellationToken);
            var robo = CreateRobo(7, 2, "Persistent Robo");
            var expected = RoboData.FromBytes(robo.ToBytes());
            expected.Character.Map = new CharacterMapData();
            expected.EmotionId = 0;
            var expectedWireData = expected.ToBytes();

            await using (var writeDb = new MainContext(options))
                await new RoboRepository(writeDb).UpsertAsync(
                    7,
                    robo,
                    TestContext.Current.CancellationToken
                );

            await using (var inspectionDb = new MainContext(options))
            {
                var entity = await inspectionDb
                    .Robos.AsNoTracking()
                    .Include(x => x.TpsBattleData)
                        .ThenInclude(x => x.BattleAbilities)
                    .Include(x => x.Equipment)
                    .Include(x => x.ItemUseEffects)
                    .Include(x => x.DistributedStatusPoints)
                    .SingleAsync(TestContext.Current.CancellationToken);
                Assert.Equal(7, entity.CharacterId);
                Assert.Equal(2u, entity.RoboId);
                Assert.Equal("Persistent Robo", entity.Name);
                Assert.Equal(44u, entity.JobId);
                Assert.Equal(21u, entity.TpsBattleData.ActionProfileId);
                Assert.Equal(24u, entity.TpsBattleData.HitPointsCurrent);
                Assert.Equal(30, entity.Equipment.Count);
                Assert.Equal(8, entity.ItemUseEffects.Count);
                Assert.Equal(20, entity.TpsBattleData.BattleAbilities.Count);
                Assert.Equal(5, entity.DistributedStatusPoints.Count);
            }

            await using var restartedDb = new MainContext(options);
            var loaded = Assert.Single(
                await new RoboRepository(restartedDb).GetAllAsync(
                    7,
                    TestContext.Current.CancellationToken
                )
            );
            Assert.Equal(expectedWireData, loaded.ToBytes());
            Assert.Equal(2u, loaded.RoboId);
            Assert.Equal(7u, loaded.OwnerAvatarId);
            Assert.Equal("Persistent Robo", loaded.Character.Name);
            Assert.Equal(44u, loaded.Character.JobId);
            Assert.Equal(0u, loaded.Character.Map.ChannelId);
            Assert.Equal(0u, loaded.Character.Map.MapId);
            Assert.Equal(0u, loaded.Character.Map.MapSerialId);
            Assert.Equal(0u, loaded.Character.Map.RouteState);
            Assert.Equal(0f, loaded.Character.Movement.X);
            Assert.Equal(0f, loaded.Character.Movement.Y);
            Assert.Equal(0f, loaded.Character.Movement.Z);
            Assert.Equal(0, loaded.Character.Movement.Rotation);
            Assert.Equal(MovementType.Stopped, loaded.Character.Movement.Animation);
            Assert.Equal(0u, loaded.EmotionId);
            Assert.Equal(6u, loaded.AvailableStatusPoints);
            Assert.Equal([1u, 2u, 3u, 4u, 5u], loaded.DistributedStatusPoints);
            Assert.Equal(9001u, loaded.ItemUseEffects[0].ItemDefinitionId);
            Assert.Equal("Online", loaded.UserStatus.StatusText);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Upsert_replaces_same_robo_without_creating_duplicate()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 8, TestContext.Current.CancellationToken);
            await using var db = new MainContext(options);
            var repository = new RoboRepository(db);
            await repository.UpsertAsync(
                8,
                CreateRobo(8, 1, "First"),
                TestContext.Current.CancellationToken
            );
            await repository.UpsertAsync(
                8,
                CreateRobo(8, 1, "Updated"),
                TestContext.Current.CancellationToken
            );

            var loaded = Assert.Single(
                await repository.GetAllAsync(8, TestContext.Current.CancellationToken)
            );
            Assert.Equal("Updated", loaded.Character.Name);
            Assert.Equal(1, await db.Robos.CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(
                1,
                await db.RoboTpsBattleData.CountAsync(TestContext.Current.CancellationToken)
            );
            Assert.Equal(
                30,
                await db.RoboEquipment.CountAsync(TestContext.Current.CancellationToken)
            );
            Assert.Equal(
                8,
                await db.RoboItemUseEffects.CountAsync(TestContext.Current.CancellationToken)
            );
            Assert.Equal(
                20,
                await db.RoboBattleAbilities.CountAsync(TestContext.Current.CancellationToken)
            );
            Assert.Equal(
                5,
                await db.RoboDistributedStatusPoints.CountAsync(
                    TestContext.Current.CancellationToken
                )
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ResetEquipmentAndRename_returns_items_to_inventory_and_applies_defaults()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 9, TestContext.Current.CancellationToken);
            const int customHat = 10000010;
            const int existingShirt = 10100060;

            await using (var seedDb = new MainContext(options))
            {
                seedDb.Items.AddRange(
                    new aisp.Common.DAL.Entities.Item { Id = customHat, Name = "Custom Hat" },
                    new aisp.Common.DAL.Entities.Item
                    {
                        Id = existingShirt,
                        Name = "Starter Shirt",
                    },
                    new aisp.Common.DAL.Entities.Item { Id = 10200090, Name = "Starter Shorts" },
                    new aisp.Common.DAL.Entities.Item { Id = 10400000, Name = "Starter Socks" },
                    new aisp.Common.DAL.Entities.Item { Id = 10500010, Name = "Starter Shoes" }
                );
                seedDb.CharacterInventories.Add(
                    new aisp.Common.DAL.Entities.CharacterInventory
                    {
                        CharacterId = 9,
                        ItemId = existingShirt,
                        Quantity = 2,
                    }
                );
                await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);

                var robo = CreateRobo(9, 1, "Old Name");
                robo.Character.Equips.Clear();
                for (byte slot = 0; slot < CharaData.EquipmentSlotCount; slot++)
                    robo.Character.Equips.Add(new ItemSlotInfo(0, 0));
                robo.Character.Equips[0] = new ItemSlotInfo((uint)existingShirt, 0);
                robo.Character.Equips[6] = new ItemSlotInfo((uint)customHat, 0);
                await new RoboRepository(seedDb).UpsertAsync(
                    9,
                    robo,
                    TestContext.Current.CancellationToken
                );

                var seededCharacter = await seedDb.Characters.SingleAsync(
                    TestContext.Current.CancellationToken
                );
                seededCharacter.HomeIslandId = 1;
                seededCharacter.CharadollPersonality = aisp.Common
                    .DAL
                    .Entities
                    .CharadollPersonality
                    .Quiet;
                await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var db = new MainContext(options);
            var repository = new RoboRepository(db);
            var ok = await repository.ResetEquipmentAndRenameAsync(
                9,
                1,
                "New Doll",
                aisp.Common.DAL.Entities.CharadollPersonality.Active,
                TestContext.Current.CancellationToken
            );
            Assert.True(ok);

            var loaded = await repository.GetAsync(9, 1, TestContext.Current.CancellationToken);
            Assert.NotNull(loaded);
            Assert.Equal("New Doll", loaded.Character.Name);
            Assert.Equal(2012030u, loaded.Character.ModelId);
            Assert.Equal(0u, loaded.Character.Visual.Hairstyle);
            Assert.Equal(88, loaded.Character.Progress.Level);

            Assert.Equal(
                [
                    (uint)DefaultClothingItems.Female[0],
                    (uint)DefaultClothingItems.Female[1],
                    (uint)DefaultClothingItems.Female[2],
                    (uint)DefaultClothingItems.Female[3],
                ],
                loaded.Character.Equips.Where(e => e.ItemId != 0).Select(e => e.ItemId).ToArray()
            );
            Assert.Equal(4, loaded.Character.Equips.Count(e => e.ItemId != 0));

            var inventory = await db
                .CharacterInventories.AsNoTracking()
                .Where(i => i.CharacterId == 9)
                .ToDictionaryAsync(
                    i => i.ItemId,
                    i => i.Quantity,
                    TestContext.Current.CancellationToken
                );
            Assert.Equal(3, inventory[existingShirt]);
            Assert.Equal(1, inventory[customHat]);

            var updatedCharacter = await db
                .Characters.AsNoTracking()
                .SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(
                aisp.Common.DAL.Entities.CharadollPersonality.Active,
                updatedCharacter.CharadollPersonality
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ResetEquipmentAndRename_returns_false_when_robo_missing()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 10, TestContext.Current.CancellationToken);
            await using var db = new MainContext(options);
            var ok = await new RoboRepository(db).ResetEquipmentAndRenameAsync(
                10,
                1,
                "Ghost",
                aisp.Common.DAL.Entities.CharadollPersonality.Quiet,
                TestContext.Current.CancellationToken
            );
            Assert.False(ok);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ResetEquipmentAndRename_applies_defaults_when_doll_has_no_extra_gear()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 11, TestContext.Current.CancellationToken);
            await using (var seedDb = new MainContext(options))
            {
                foreach (var itemId in DefaultClothingItems.Female)
                    seedDb.Items.Add(
                        new aisp.Common.DAL.Entities.Item { Id = itemId, Name = $"item-{itemId}" }
                    );
                await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);

                var robo = CreateRobo(11, 1, "Bare");
                robo.Character.Equips.Clear();
                for (byte slot = 0; slot < CharaData.EquipmentSlotCount; slot++)
                    robo.Character.Equips.Add(new ItemSlotInfo(0, 0));
                await new RoboRepository(seedDb).UpsertAsync(
                    11,
                    robo,
                    TestContext.Current.CancellationToken
                );

                var character = await seedDb.Characters.SingleAsync(
                    TestContext.Current.CancellationToken
                );
                character.HomeIslandId = 2;
                await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var db = new MainContext(options);
            var repository = new RoboRepository(db);
            Assert.True(
                await repository.ResetEquipmentAndRenameAsync(
                    11,
                    1,
                    "Renamed Bare",
                    aisp.Common.DAL.Entities.CharadollPersonality.Quiet,
                    TestContext.Current.CancellationToken
                )
            );

            var loaded = await repository.GetAsync(11, 1, TestContext.Current.CancellationToken);
            Assert.NotNull(loaded);
            Assert.Equal("Renamed Bare", loaded.Character.Name);
            Assert.Equal(2022030u, loaded.Character.ModelId);
            Assert.Equal(4, loaded.Character.Equips.Count(e => e.ItemId != 0));
            Assert.Equal((uint)DefaultClothingItems.Female[0], loaded.Character.Equips[0].ItemId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static RoboData CreateRobo(uint characterId, uint roboId, string name)
    {
        var objectId = RoboRepository.GetObjectId(characterId, roboId);
        var character = new CharaData(objectId, 1002011, name)
        {
            Visual = new CharaVisual(BloodType.AB, 6, 7, 2, objectId, 3, 10930010),
            CharacterParameterId = 11,
            Map = new CharacterMapData
            {
                ChannelId = 12,
                MapId = 40990200,
                MapSerialId = 13,
                RouteState = 14,
                Movement = new MovementData(15.5f, 16.5f, 17.5f, 180, MovementType.Running),
            },
            TpsActionReferenceX = 18.5f,
            TpsActionReferenceY = 19.5f,
            JobId = 44,
            TpsActionProfileId = 21,
            CollisionRadius = 22.5f,
            TpsActionVerticalRange = 23.5f,
            Battle = new TpsBattleData
            {
                HitPoints = new HitPointData
                {
                    Current = 24,
                    BaseMaximum = 25,
                    MaximumBonus = 26,
                    MaximumPenalty = 27,
                    CurrentHearts = 3,
                    MaximumHearts = 4,
                },
                Stamina = new StaminaData
                {
                    Current = 28.5f,
                    RecoveryRate = 29.5f,
                    CostReductionBonus = 30,
                    CostReductionPenalty = 31,
                },
                Tank = new TankData
                {
                    Current = 32,
                    BaseMaximum = 33,
                    MaximumBonus = 34,
                    MaximumPenalty = 35,
                },
                BaseAbilities = AbilityValues(40),
                AbilityModifierType0 = AbilityValues(50),
                AbilityModifierType1 = AbilityValues(60),
                AbilityModifierType2 = AbilityValues(70),
                StatusEffectFlags = ulong.MaxValue,
                ActionFlags = 81,
                ActiveSkillId = 82,
                Cosplay = new CosplayProgressData
                {
                    CosplayId = 83,
                    Progress = new LevelProgressData
                    {
                        Level = 84,
                        StatusPoints = ulong.MaxValue - 1,
                        Experience = ulong.MaxValue - 2,
                        ExperienceToNextLevel = ulong.MaxValue - 3,
                    },
                },
            },
            Progress = new LevelProgressData
            {
                Level = 88,
                StatusPoints = ulong.MaxValue - 4,
                Experience = ulong.MaxValue - 5,
                ExperienceToNextLevel = ulong.MaxValue - 6,
            },
        };
        for (uint slot = 0; slot < CharaData.EquipmentSlotCount; slot++)
            character.AddEquip(10100000 + slot, 200 + slot);

        var robo = new RoboData(roboId, character, state: 1)
        {
            OwnerAvatarId = characterId,
            AiScriptId = 93,
            EmotionId = 55,
            AvailableStatusPoints = 6,
            DistributedStatusPoints = [1, 2, 3, 4, 5],
            UserStatus = new UserStatusData { StatusText = "Online", StatusIconId = 9 },
        };
        for (var slot = 0; slot < RoboData.ItemUseEffectCount; slot++)
        {
            var slotValue = (uint)slot;
            robo.ItemUseEffects[slot] = new ItemUseEffectData
            {
                ItemSerialId = 100 + slotValue,
                Enabled = 1,
                ItemDefinitionId = 9001 + slotValue,
                EffectType = 200 + slotValue,
                Parameters =
                [
                    300 + slotValue,
                    400 + slotValue,
                    500 + slotValue,
                    600 + slotValue,
                    700 + slotValue,
                ],
                OverwriteExisting = (byte)(slot % 2),
            };
        }
        return robo;
    }

    private static BattleAbilityValues AbilityValues(uint firstValue)
    {
        return new BattleAbilityValues
        {
            Values = [firstValue, firstValue + 1, firstValue + 2, firstValue + 3, firstValue + 4],
        };
    }
}
