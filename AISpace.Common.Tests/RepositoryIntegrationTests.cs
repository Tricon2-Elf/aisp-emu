using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Network.Data;
using AISpace.Common.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AISpace.Common.Tests;

public class RepositoryIntegrationTests
{
    [Fact]
    public async Task UserRepository_Add_GetByUsername_VerifyPassword()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var repo = new UserRepository(new MainContext(options));
            await repo.AddAsync("alice", "secret");
            var user = await repo.GetByUsernameAsync("alice");
            Assert.NotNull(user);
            Assert.True(user.VerifyPassword("secret"));
            Assert.False(user.VerifyPassword("wrong"));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task WorldRepository_Add_GetAll_GetById()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var repo = new WorldRepository(new MainContext(options));
            await repo.AddAsync("w1", "desc", "127.0.0.1", 50052);
            var all = await repo.GetAllAsync();
            Assert.Single(all);
            var id = all[0].Id;
            var w = await repo.GetByIdAsync(id);
            Assert.NotNull(w);
            Assert.Equal("w1", w!.Name);
            Assert.Equal("127.0.0.1", w.Address);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UserSessionRepository_Create_GetValidSession()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var userRepo = new UserRepository(new MainContext(options));
            await userRepo.AddAsync("bob", "pw");
            var user = await userRepo.GetByUsernameAsync("bob");
            Assert.NotNull(user);

            var db = new MainContext(options);
            var sessionRepo = new UserSessionRepository(db, NullLogger<UserSessionRepository>.Instance);
            const string otp = "1234567890123456";

            await sessionRepo.CreateAsync(user!.Id, otp, TimeSpan.FromHours(1), TestContext.Current.CancellationToken);

            var valid = await sessionRepo.GetValidSessionAsync(otp, TestContext.Current.CancellationToken);
            Assert.NotNull(valid);
            Assert.Equal(user.Id, valid!.UserId);
            Assert.NotNull(valid.User);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UserSessionRepository_GetValidSession_ReturnsNull_WhenExpired()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var userRepo = new UserRepository(new MainContext(options));
            await userRepo.AddAsync("carl", "pw");
            var user = await userRepo.GetByUsernameAsync("carl");

            var db = new MainContext(options);
            var sessionRepo = new UserSessionRepository(db, NullLogger<UserSessionRepository>.Instance);
            const string otp = "abcdefghijklmnop";

            await sessionRepo.CreateAsync(user!.Id, otp, TimeSpan.FromHours(1), TestContext.Current.CancellationToken);

            await using (var ctx = new MainContext(options))
            {
                var s = await ctx.UserSessions.SingleAsync(TestContext.Current.CancellationToken);
                s.ExpiresAt = DateTime.UtcNow.AddHours(-1);
                await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            Assert.Null(await sessionRepo.GetValidSessionAsync(otp, TestContext.Current.CancellationToken));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task MapRepository_EnsureSeedMapsPresent_AddsMissingCanonicalMaps_ToExistingDatabase()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                db.Maps.Add(
                    new AISpace.Common.DAL.Entities.Map
                    {
                        MapId = 10990100,
                        Name = "Akihabara",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 90,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using (var db = new MainContext(options))
            {
                await MapRepository.EnsureSeedMapsPresentAsync(db, Path.Combine(AppContext.BaseDirectory, "seedData", "maps.json"), TestContext.Current.CancellationToken);
            }

            await using (var verifyDb = new MainContext(options))
            {
                Assert.NotNull(await verifyDb.Maps.FirstOrDefaultAsync(map => map.MapId == 10990200, TestContext.Current.CancellationToken));
                Assert.NotNull(await verifyDb.Maps.FirstOrDefaultAsync(map => map.MapId == 10990210, TestContext.Current.CancellationToken));
            }
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task CharacterRepository_ReplaceEquipmentAsync_RejectsUnownedItem_WithoutPersistingChanges()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int characterId = 9001;
            const int oldTopId = 10100060;
            const int requestedTopId = 10100220;

            await using (var db = new MainContext(options))
            {
                var user = new User { Id = 1, Username = "equip-owner-check" };
                user.SetPassword("pw");
                db.Users.Add(user);

                db.Characters.Add(
                    new Character
                    {
                        Id = characterId,
                        UserId = user.Id,
                        Name = "Equip Check",
                        ModelId = 100,
                        Birthdate = new DateTime(2000, 1, 1),
                        BloodType = BloodType.A,
                        Gender = 1,
                        FaceType = 1,
                        Hairstyle = 1,
                        CurrentMapId = 10990100,
                    }
                );

                db.Items.AddRange(new Item { Id = oldTopId, Name = "Old Top", Socket = 8 }, new Item { Id = requestedTopId, Name = "Requested Top", Socket = 8 });
                db.CharacterEquipments.Add(new CharacterEquipment { CharacterId = characterId, SlotIndex = 1, ItemId = oldTopId });

                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var repo = new CharacterRepository(new MainContext(options), NullLogger<CharacterRepository>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repo.ReplaceEquipmentAsync(characterId, [new ItemEquipEntry((uint)requestedTopId, 8)], TestContext.Current.CancellationToken)
            );

            await using (var verifyDb = new MainContext(options))
            {
                var equips = await verifyDb
                    .CharacterEquipments.Where(x => x.CharacterId == characterId)
                    .OrderBy(x => x.SlotIndex)
                    .ToListAsync(TestContext.Current.CancellationToken);

                Assert.Single(equips);
                Assert.Equal(oldTopId, equips[0].ItemId);

                var inventories = await verifyDb.CharacterInventories.Where(x => x.CharacterId == characterId).ToListAsync(TestContext.Current.CancellationToken);
                Assert.Empty(inventories);
            }
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task CharacterRepository_ReplaceEquipmentAsync_PreservesModestySlotsWhenPayloadOmitsThem()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int characterId = 9010;
            const int topId = 10100220;
            const int bottomId = 10200100;
            const int socksId = 10400030;
            const int shoesId = 10500070;
            const int braId = 10600000;
            const int lowerUnderwearId = 10700020;
            const int hatId = 10000050;

            await using (var db = new MainContext(options))
            {
                var user = new User { Id = 2, Username = "modesty-preserve-check" };
                user.SetPassword("pw");
                db.Users.Add(user);

                db.Characters.Add(
                    new Character
                    {
                        Id = characterId,
                        UserId = user.Id,
                        Name = "Modesty Check",
                        ModelId = 100,
                        Birthdate = new DateTime(2000, 1, 1),
                        BloodType = BloodType.A,
                        Gender = 1,
                        FaceType = 1,
                        Hairstyle = 1,
                        CurrentMapId = 10990100,
                    }
                );

                db.Items.AddRange(
                    new Item { Id = topId, Name = "Top", Socket = 8 },
                    new Item { Id = bottomId, Name = "Bottom", Socket = 32 },
                    new Item { Id = socksId, Name = "Socks", Socket = 128 },
                    new Item { Id = shoesId, Name = "Shoes", Socket = 512 },
                    new Item { Id = braId, Name = "Bra", Socket = 1024 },
                    new Item { Id = lowerUnderwearId, Name = "Underwear", Socket = 2048 },
                    new Item { Id = hatId, Name = "Hardhat", Socket = 10 }
                );

                db.CharacterEquipments.AddRange(
                    new CharacterEquipment { CharacterId = characterId, SlotIndex = 0, ItemId = topId },
                    new CharacterEquipment { CharacterId = characterId, SlotIndex = 1, ItemId = bottomId },
                    new CharacterEquipment { CharacterId = characterId, SlotIndex = 2, ItemId = socksId },
                    new CharacterEquipment { CharacterId = characterId, SlotIndex = 3, ItemId = shoesId },
                    new CharacterEquipment { CharacterId = characterId, SlotIndex = 4, ItemId = lowerUnderwearId },
                    new CharacterEquipment { CharacterId = characterId, SlotIndex = 5, ItemId = braId }
                );
                db.CharacterInventories.Add(new CharacterInventory { CharacterId = characterId, ItemId = hatId, Quantity = 1 });

                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var repo = new CharacterRepository(new MainContext(options), NullLogger<CharacterRepository>.Instance);

            await repo.ReplaceEquipmentAsync(
                characterId,
                [
                    new ItemEquipEntry((uint)topId, 8),
                    new ItemEquipEntry((uint)bottomId, 32),
                    new ItemEquipEntry((uint)socksId, 128),
                    new ItemEquipEntry((uint)shoesId, 512),
                    new ItemEquipEntry((uint)hatId, 1),
                ],
                TestContext.Current.CancellationToken
            );

            await using (var verifyDb = new MainContext(options))
            {
                var equips = await verifyDb
                    .CharacterEquipments.Where(x => x.CharacterId == characterId)
                    .OrderBy(x => x.SlotIndex)
                    .ToListAsync(TestContext.Current.CancellationToken);

                Assert.Contains(equips, x => x.SlotIndex == 4 && x.ItemId == lowerUnderwearId);
                Assert.Contains(equips, x => x.SlotIndex == 5 && x.ItemId == braId);
                Assert.Contains(equips, x => x.SlotIndex == 6 && x.ItemId == hatId);
            }
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
