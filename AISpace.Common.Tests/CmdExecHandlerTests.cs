using AISpace.Common.Config;
using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Handlers.Msg;
using AISpace.Common.Services;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using AISpace.Network.Packets.Msg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AISpace.Common.Tests;

public class CmdExecHandlerTests
{
    [Fact]
    public async Task TeleCommand_MovesAreaSessionToRequestedMap()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 8001, "tele-user", "Tele User", 10990100);

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Channels.Add(
                    new GameChannel
                    {
                        ChannelNum = 1,
                        IP = "localhost",
                        Port = 50054,
                        MapId = 10990100,
                    }
                );
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Akihabara",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 90,
                    },
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Akihabara 2",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var areaSession = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
                Character = user.Characters.First(),
                CharacterId = 8001,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, areaSession);

            var msgSession = new CapturingPlayerSession { User = user, UserId = user.Id };

            var handler = new CmdExecHandler(state, new MapRepository(new MainContext(options)), new UserRepository(new MainContext(options)), new CharacterRepository(new MainContext(options), NullLogger<CharacterRepository>.Instance), new StubItemBaseListCache(), CreateDirectMapLinkTransitionService(options, state), NullLogger<CmdExecHandler>.Instance);

            await handler.HandleAsync(BuildCmdExecPayload("tele", "10990110"), msgSession, TestContext.Current.CancellationToken);

            Assert.Equal(10990110u, areaSession.MapId);
            Assert.Equal(-11000f, areaSession.X);
            Assert.Equal(0.1f, areaSession.Y);
            Assert.Equal(-19200f, areaSession.Z);
            Assert.Equal(10990110u, areaSession.Character!.CurrentMapId);
            Assert.Contains(areaSession.Sent, packet => packet.Type == PacketType.NotifyChangeMap);
            Assert.Contains(msgSession.Sent, packet => packet.Type == PacketType.CmdExecResponse);

            await using var verifyDb = new MainContext(options);
            var persisted = await verifyDb.Characters.SingleAsync(c => c.Id == 8001, TestContext.Current.CancellationToken);
            Assert.Equal(10990110u, persisted.CurrentMapId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task MyRoomCommand_TeleportsAreaSessionToBaseMyRoomMap()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 8008, "myroom-user", "MyRoom User", 10990100);

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Channels.Add(
                    new GameChannel
                    {
                        ChannelNum = 1,
                        IP = "localhost",
                        Port = 50054,
                        MapId = 10990100,
                    }
                );
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Akihabara",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 90,
                    },
                    new Map
                    {
                        MapId = MyRoomInfo.BaseMapId,
                        Name = "My Room (6 tatami mats)",
                        SpawnX = 0f,
                        SpawnY = 0.1f,
                        SpawnZ = 0f,
                        SpawnRotation = 0,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var areaSession = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
                Character = user.Characters.First(),
                CharacterId = 8008,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, areaSession);

            var msgSession = new CapturingPlayerSession { User = user, UserId = user.Id };

            var handler = new CmdExecHandler(state, new MapRepository(new MainContext(options)), new UserRepository(new MainContext(options)), new CharacterRepository(new MainContext(options), NullLogger<CharacterRepository>.Instance), new StubItemBaseListCache(), CreateDirectMapLinkTransitionService(options, state), NullLogger<CmdExecHandler>.Instance);

            await handler.HandleAsync(BuildCmdExecPayload("/room"), msgSession, TestContext.Current.CancellationToken);

            Assert.Equal(MyRoomInfo.BaseMapId, areaSession.MapId);
            Assert.Equal(0f, areaSession.X);
            Assert.Equal(0.1f, areaSession.Y);
            Assert.Equal(0f, areaSession.Z);
            Assert.Equal(MyRoomInfo.BaseMapId, areaSession.Character!.CurrentMapId);
            Assert.Contains(areaSession.Sent, packet => packet.Type == PacketType.NotifyChangeMyRoom);
            Assert.Contains(msgSession.Sent, packet => packet.Type == PacketType.CmdExecResponse);

            await using var verifyDb = new MainContext(options);
            var persisted = await verifyDb.Characters.SingleAsync(c => c.Id == 8008, TestContext.Current.CancellationToken);
            Assert.Equal(MyRoomInfo.BaseMapId, persisted.CurrentMapId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task JumpCommand_MovesAreaSessionForwardAlongRotation()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 8003, "jump-user", "Jump User", 10990100);
            var state = new SharedState();
            var areaSession = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
                Character = user.Characters.First(),
                CharacterId = 8003,
                MapId = 10990100,
                ChannelId = 1,
                X = 100f,
                Y = 2f,
                Z = 200f,
                Rotation = 90,
            };
            state.RegisterClient(ServerType.Area, areaSession);

            var msgSession = new CapturingPlayerSession { User = user, UserId = user.Id };

            var handler = new CmdExecHandler(state, new MapRepository(new MainContext(options)), new UserRepository(new MainContext(options)), new CharacterRepository(new MainContext(options), NullLogger<CharacterRepository>.Instance), new StubItemBaseListCache(), CreateDirectMapLinkTransitionService(options, state), NullLogger<CmdExecHandler>.Instance);

            await handler.HandleAsync(BuildCmdExecPayload("jump"), msgSession, TestContext.Current.CancellationToken);

            Assert.Equal(100f, areaSession.X, precision: 3);
            Assert.Equal(2f, areaSession.Y, precision: 3);
            Assert.Equal(100f, areaSession.Z, precision: 3);

            areaSession.Z = 200f;
            await handler.HandleAsync(BuildCmdExecPayload("jump", "50"), msgSession, TestContext.Current.CancellationToken);

            Assert.Equal(150f, areaSession.Z, precision: 3);
            Assert.Contains(areaSession.Sent, packet => packet.Type == PacketType.AvatarNotifyMove);
            Assert.Contains(msgSession.Sent, packet => packet.Type == PacketType.CmdExecResponse);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task OutfitCommand_AddsDefaultClothingToInventoryAndNotifiesAreaClient()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 8004, "outfit-user", "Outfit User", 10990100);

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                foreach (var itemId in DefaultClothingItems.Male)
                {
                    db.Items.Add(
                        new Item
                        {
                            Id = itemId,
                            Name = $"item-{itemId}",
                            IconId = itemId,
                            Socket = 0,
                        }
                    );
                }
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var areaSession = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
                Character = user.Characters.First(),
                CharacterId = 8004,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, areaSession);

            var msgSession = new CapturingPlayerSession { User = user, UserId = user.Id };

            var handler = new CmdExecHandler(state, new MapRepository(new MainContext(options)), new UserRepository(new MainContext(options)), new CharacterRepository(new MainContext(options), NullLogger<CharacterRepository>.Instance), new StubItemBaseListCache(DefaultClothingItems.Male), CreateDirectMapLinkTransitionService(options, state), NullLogger<CmdExecHandler>.Instance);

            await handler.HandleAsync(BuildCmdExecPayload("outfit"), msgSession, TestContext.Current.CancellationToken);

            await using var verifyDb = new MainContext(options);
            var inventory = await verifyDb.CharacterInventories.Where(i => i.CharacterId == 8004).ToListAsync(TestContext.Current.CancellationToken);
            var expectedItems = DefaultClothingItems.WardrobeInventoryForGender(1).ToList();
            Assert.Equal(expectedItems.Count, inventory.Count);
            Assert.All(expectedItems, itemId => Assert.Contains(inventory, i => i.ItemId == itemId && i.Quantity == 1));
            Assert.Contains(areaSession.Sent, p => p.Type == PacketType.ItemGetListResponse);
            Assert.Equal(expectedItems.Count, areaSession.Sent.Count(p => p.Type == PacketType.ItemCreateNotify));
            Assert.Equal(expectedItems.Count, areaSession.Sent.Count(p => p.Type == PacketType.ItemUpdateListNotify));
            Assert.Contains(msgSession.Sent, packet => packet.Type == PacketType.CmdExecResponse);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GiveCommand_AddsItemToInventoryAndSendsItemCreateNotify()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 8005, "give-user", "Give User", 10990100);
            const int itemId = 1201001;

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Items.Add(
                    new Item
                    {
                        Id = itemId,
                        Name = "Give Item",
                        IconId = itemId,
                        Socket = 0,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var areaSession = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
                Character = user.Characters.First(),
                CharacterId = 8005,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, areaSession);

            var msgSession = new CapturingPlayerSession { User = user, UserId = user.Id };

            var handler = new CmdExecHandler(state, new MapRepository(new MainContext(options)), new UserRepository(new MainContext(options)), new CharacterRepository(new MainContext(options), NullLogger<CharacterRepository>.Instance), new StubItemBaseListCache([itemId]), CreateDirectMapLinkTransitionService(options, state), NullLogger<CmdExecHandler>.Instance);

            await handler.HandleAsync(BuildCmdExecPayload("/give", itemId.ToString()), msgSession, TestContext.Current.CancellationToken);

            await using var verifyDb = new MainContext(options);
            var inventory = await verifyDb.CharacterInventories.SingleAsync(i => i.CharacterId == 8005 && i.ItemId == itemId, TestContext.Current.CancellationToken);
            Assert.Equal(1, inventory.Quantity);

            Assert.Equal(1, areaSession.Sent.Count(p => p.Type == PacketType.ItemCreateNotify));
            Assert.Equal(1, areaSession.Sent.Count(p => p.Type == PacketType.ItemUpdateListNotify));
            Assert.Contains(msgSession.Sent, packet => packet.Type == PacketType.CmdExecResponse);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GiveCommand_RejectsItemNotInItemBaseListCache()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 8006, "give-missing-user", "Give Missing User", 10990100);
            const int itemId = 1201999;

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Items.Add(
                    new Item
                    {
                        Id = itemId,
                        Name = "Missing In Cache",
                        IconId = itemId,
                        Socket = 0,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var areaSession = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
                Character = user.Characters.First(),
                CharacterId = 8006,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, areaSession);

            var msgSession = new CapturingPlayerSession { User = user, UserId = user.Id };

            var handler = new CmdExecHandler(state, new MapRepository(new MainContext(options)), new UserRepository(new MainContext(options)), new CharacterRepository(new MainContext(options), NullLogger<CharacterRepository>.Instance), new StubItemBaseListCache(), CreateDirectMapLinkTransitionService(options, state), NullLogger<CmdExecHandler>.Instance);

            await handler.HandleAsync(BuildCmdExecPayload("/give", itemId.ToString()), msgSession, TestContext.Current.CancellationToken);

            await using var verifyDb = new MainContext(options);
            var inventoryCount = await verifyDb.CharacterInventories.CountAsync(i => i.CharacterId == 8006 && i.ItemId == itemId, TestContext.Current.CancellationToken);
            Assert.Equal(0, inventoryCount);
            Assert.DoesNotContain(areaSession.Sent, p => p.Type == PacketType.ItemCreateNotify);
            Assert.Contains(msgSession.Sent, packet => packet.Type == PacketType.CmdExecResponse);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task MoneyCommand_AddsRequestedCurrencyAndSendsBalanceNotifies()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 8007, "money-user", "Money User", 10990100);
            user.AiPoints = 10;
            user.NicoPoints = 20;

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var areaSession = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
                Character = user.Characters.First(),
                CharacterId = 8007,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, areaSession);

            var msgSession = new CapturingPlayerSession { User = user, UserId = user.Id };

            var handler = new CmdExecHandler(state, new MapRepository(new MainContext(options)), new UserRepository(new MainContext(options)), new CharacterRepository(new MainContext(options), NullLogger<CharacterRepository>.Instance), new StubItemBaseListCache(), CreateDirectMapLinkTransitionService(options, state), NullLogger<CmdExecHandler>.Instance);

            await handler.HandleAsync(BuildCmdExecPayload("/money", "50", "nico"), msgSession, TestContext.Current.CancellationToken);

            await using var verifyDb = new MainContext(options);
            var updated = await verifyDb.Users.SingleAsync(u => u.Id == user.Id, TestContext.Current.CancellationToken);
            Assert.Equal(10, updated.AiPoints);
            Assert.Equal(70, updated.NicoPoints);

            Assert.Contains(areaSession.Sent, p => p.Type == PacketType.MoneyUpdatedAipoint);
            Assert.Contains(areaSession.Sent, p => p.Type == PacketType.MoneyUpdatedNicopoint);
            Assert.Contains(msgSession.Sent, packet => packet.Type == PacketType.CmdExecResponse);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task EventScriptPlayR_Success_SendsFadeInAndWaitsForAck()
    {
        var areaSession = new CapturingPlayerSession { CharacterId = 1, UserId = 2 };
        var handler = new AreaEventScriptPlayHandler(NullLogger<AreaEventScriptPlayHandler>.Instance);

        var writer = new PacketWriter();
        writer.Write(0);
        await handler.HandleAsync(writer.ToBytes(), areaSession, TestContext.Current.CancellationToken);

        Assert.True(areaSession.PendingEventEndAfterFade);
        var fade = Assert.Single(areaSession.Sent);
        Assert.Equal(PacketType.EventFadeInNotify, fade.Type);
        Assert.Equal(new EventFadeNotify(1f, 255, 255, 255).ToBytes(), fade.Payload);
    }

    [Fact]
    public async Task EventFadeInR_AfterScriptPlay_SendsEventEnd()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var areaSession = new CapturingPlayerSession
            {
                CharacterId = 1,
                UserId = 2,
                PendingEventEndAfterFade = true,
            };
            await using var db = new MainContext(options);
            var eventRepo = new CharacterEventRepository(db);
            var handler = new AreaEventFadeInHandler(eventRepo, NullLogger<AreaEventFadeInHandler>.Instance);

            await handler.HandleAsync(ReadOnlyMemory<byte>.Empty, areaSession, TestContext.Current.CancellationToken);

            Assert.False(areaSession.PendingEventEndAfterFade);
            var end = Assert.Single(areaSession.Sent);
            Assert.Equal(PacketType.EventEndNotify, end.Type);
            Assert.Equal(new EventEndNotify(0).ToBytes(), end.Payload);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PosCommand_ResolvesLiveAreaSession_NotPersistedPresenceSnapshot()
    {
        var user = CreateUserWithCharacter(1, 8002, "pos-user", "Pos User", 10990100);
        var state = new SharedState();
        var areaSession = new CapturingPlayerSession
        {
            User = user,
            UserId = user.Id,
            CharacterId = 8002,
            X = -9100f,
            Z = -18000f,
        };
        state.RegisterClient(ServerType.Area, areaSession);

        areaSession.X = -9055.5f;
        areaSession.Z = -17988.75f;

        var resolved = state.GetAreaSessionByUserId(user.Id);
        Assert.Same(areaSession, resolved);
        Assert.Equal(-9055.5f, resolved!.X);
        Assert.Equal(-17988.75f, resolved.Z);
    }

    private static byte[] BuildCmdExecPayload(string command, params string[] args)
    {
        var writer = new PacketWriter();
        writer.Write(1u);
        writer.WriteFixedString(command, 96, "ASCII");
        for (var i = 0; i < 10; i++)
            writer.WriteFixedString(i < args.Length ? args[i] : string.Empty, 384, "ASCII");
        writer.Write((uint)args.Length);
        return writer.ToBytes();
    }

    private static User CreateUserWithCharacter(int userId, int characterId, string username, string characterName, uint currentMapId)
    {
        var user = new User { Id = userId, Username = username };
        user.SetPassword("pw");
        user.Characters.Add(
            new Character
            {
                Id = characterId,
                Name = characterName,
                UserId = userId,
                CurrentMapId = currentMapId,
                ModelId = 100,
                Birthdate = new DateTime(2000, 1, 2),
                BloodType = BloodType.A,
                Gender = 1,
                FaceType = 1,
                Hairstyle = 2,
            }
        );
        return user;
    }

    private static DirectMapLinkTransitionService CreateDirectMapLinkTransitionService(DbContextOptions<MainContext> options, SharedState state)
    {
        return new DirectMapLinkTransitionService(
            new MapRepository(new MainContext(options)),
            new CharacterRepository(new MainContext(options), NullLogger<CharacterRepository>.Instance),
            new MapLinkRepository(new MainContext(options)),
            new ChannelRepository(new MainContext(options)),
            Options.Create(
                new ServerOptions
                {
                    NetworkOptions = new NetworkOptions(),
                    DbOptions = new DbOptions(),
                    IPOverride = "localhost",
                }
            ),
            state,
            NullLogger<DirectMapLinkTransitionService>.Instance
        );
    }

    private sealed class StubItemBaseListCache(IEnumerable<int>? itemIds = null) : IItemBaseListCache
    {
        private readonly HashSet<int> _itemIds = itemIds?.ToHashSet() ?? [];

        public ReadOnlyMemory<byte> ResponsePayload => ReadOnlyMemory<byte>.Empty;

        public Task WarmAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> ContainsItemAsync(int itemId, CancellationToken ct = default) => Task.FromResult(_itemIds.Contains(itemId));
    }
}
