using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace aisp.Common.Tests;

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
            var sessionRepo = new UserSessionRepository(
                db,
                NullLogger<UserSessionRepository>.Instance
            );
            const string otp = "1234567890123456";

            await sessionRepo.CreateAsync(
                user!.Id,
                otp,
                TimeSpan.FromHours(1),
                TestContext.Current.CancellationToken
            );

            var valid = await sessionRepo.GetValidSessionAsync(
                otp,
                TestContext.Current.CancellationToken
            );
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
            var sessionRepo = new UserSessionRepository(
                db,
                NullLogger<UserSessionRepository>.Instance
            );
            const string otp = "abcdefghijklmnop";

            await sessionRepo.CreateAsync(
                user!.Id,
                otp,
                TimeSpan.FromHours(1),
                TestContext.Current.CancellationToken
            );

            await using (var ctx = new MainContext(options))
            {
                var s = await ctx.UserSessions.SingleAsync(TestContext.Current.CancellationToken);
                s.ExpiresAt = DateTime.UtcNow.AddHours(-1);
                await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            Assert.Null(
                await sessionRepo.GetValidSessionAsync(otp, TestContext.Current.CancellationToken)
            );
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
                    new aisp.Common.DAL.Entities.Map
                    {
                        MapId = 10990100,
                        Name = "Akihabara",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 180,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using (var db = new MainContext(options))
            {
                await MapRepository.EnsureSeedMapsPresentAsync(
                    db,
                    Path.Combine(AppContext.BaseDirectory, "seedData", "maps.json"),
                    TestContext.Current.CancellationToken
                );
            }

            await using (var verifyDb = new MainContext(options))
            {
                Assert.NotNull(
                    await verifyDb.Maps.FirstOrDefaultAsync(
                        map => map.MapId == 10990200,
                        TestContext.Current.CancellationToken
                    )
                );
                Assert.NotNull(
                    await verifyDb.Maps.FirstOrDefaultAsync(
                        map => map.MapId == 10990210,
                        TestContext.Current.CancellationToken
                    )
                );
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

                db.Items.AddRange(
                    new Item
                    {
                        Id = oldTopId,
                        Name = "Old Top",
                        Socket = 8,
                    },
                    new Item
                    {
                        Id = requestedTopId,
                        Name = "Requested Top",
                        Socket = 8,
                    }
                );
                db.CharacterEquipments.Add(
                    new CharacterEquipment
                    {
                        CharacterId = characterId,
                        SlotIndex = 1,
                        ItemId = oldTopId,
                    }
                );

                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var repo = new CharacterRepository(
                new MainContext(options),
                NullLogger<CharacterRepository>.Instance
            );

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repo.ReplaceEquipmentAsync(
                    characterId,
                    [new ItemEquipEntry((uint)requestedTopId, 8)],
                    TestContext.Current.CancellationToken
                )
            );

            await using (var verifyDb = new MainContext(options))
            {
                var equips = await verifyDb
                    .CharacterEquipments.Where(x => x.CharacterId == characterId)
                    .OrderBy(x => x.SlotIndex)
                    .ToListAsync(TestContext.Current.CancellationToken);

                Assert.Single(equips);
                Assert.Equal(oldTopId, equips[0].ItemId);

                var inventories = await verifyDb
                    .CharacterInventories.Where(x => x.CharacterId == characterId)
                    .ToListAsync(TestContext.Current.CancellationToken);
                Assert.Empty(inventories);
            }
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
