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
                        SpawnRotation = 180,
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

            var handler = new CmdExecHandler(
                state,
                new MapRepository(new MainContext(options)),
                new UserRepository(new MainContext(options)),
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                new MyRoomRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                NullLogger<CmdExecHandler>.Instance
            );

            await handler.HandleAsync(
                BuildCmdExecPayload("tele", "10990110"),
                msgSession,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(10990110u, areaSession.MapId);
            Assert.Equal(-11000f, areaSession.X);
            Assert.Equal(0.1f, areaSession.Y);
            Assert.Equal(-19200f, areaSession.Z);
            Assert.Equal(10990110u, areaSession.Character!.CurrentMapId);
            Assert.Contains(areaSession.Sent, packet => packet.Type == PacketType.NotifyChangeMap);
            Assert.Contains(msgSession.Sent, packet => packet.Type == PacketType.CmdExecResponse);

            await using var verifyDb = new MainContext(options);
            var persisted = await verifyDb.Characters.SingleAsync(
                c => c.Id == 8001,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(10990110u, persisted.CurrentMapId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("/myroom")]
    [InlineData("/room")]
    public async Task MyRoomCommand_TeleportsRegisteredAreaSessionToBaseMyRoomMap(string command)
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 8008, "myroom-user", "MyRoom User", 10990100);
            user.Characters.First().HomeIslandId = 1;

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
                        SpawnRotation = 180,
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

            var handler = new CmdExecHandler(
                state,
                new MapRepository(new MainContext(options)),
                new UserRepository(new MainContext(options)),
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                new MyRoomRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                NullLogger<CmdExecHandler>.Instance
            );

            await handler.HandleAsync(
                BuildCmdExecPayload(command),
                msgSession,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(MyRoomInfo.BaseMapId, areaSession.MapId);
            Assert.Equal(0f, areaSession.X);
            Assert.Equal(0.1f, areaSession.Y);
            Assert.Equal(0f, areaSession.Z);
            Assert.Equal(MyRoomInfo.BaseMapId, areaSession.Character!.CurrentMapId);
            Assert.Contains(
                areaSession.Sent,
                packet => packet.Type == PacketType.NotifyChangeMyRoom
            );
            Assert.Contains(msgSession.Sent, packet => packet.Type == PacketType.CmdExecResponse);

            await using var verifyDb = new MainContext(options);
            var persisted = await verifyDb.Characters.SingleAsync(
                c => c.Id == 8008,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(MyRoomInfo.BaseMapId, persisted.CurrentMapId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("/myroom")]
    [InlineData("/room")]
    public async Task MyRoomCommand_DoesNotTeleportCharacterWithoutHomeIsland(string command)
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(
                1,
                8009,
                "unregistered-myroom-user",
                "Unregistered MyRoom User",
                10990100
            );

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
                    new Map { MapId = 10990100, Name = "Akihabara" },
                    new Map { MapId = MyRoomInfo.BaseMapId, Name = "My Room (6 tatami mats)" }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var areaSession = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
                Character = user.Characters.First(),
                CharacterId = 8009,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, areaSession);

            var msgSession = new CapturingPlayerSession { User = user, UserId = user.Id };
            var handler = new CmdExecHandler(
                state,
                new MapRepository(new MainContext(options)),
                new UserRepository(new MainContext(options)),
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                new MyRoomRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                NullLogger<CmdExecHandler>.Instance
            );

            await handler.HandleAsync(
                BuildCmdExecPayload(command),
                msgSession,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(10990100u, areaSession.MapId);
            Assert.Equal(10990100u, areaSession.Character!.CurrentMapId);
            Assert.DoesNotContain(
                areaSession.Sent,
                packet => packet.Type is PacketType.NotifyChangeMap or PacketType.NotifyChangeMyRoom
            );
            Assert.Contains(msgSession.Sent, packet => packet.Type == PacketType.CmdExecResponse);

            await using var verifyDb = new MainContext(options);
            var persisted = await verifyDb.Characters.SingleAsync(
                c => c.Id == 8009,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(10990100u, persisted.CurrentMapId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task RoomCommand_VisitsAnotherCharactersRoomUsingItsConfiguredStage()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var visitor = CreateUserWithCharacter(
                1,
                8101,
                "room-visitor",
                "Room Visitor",
                10990100
            );
            visitor.Characters.First().HomeIslandId = 1;
            var owner = CreateUserWithCharacter(2, 8102, "room-owner", "Room Owner", 10990100);

            await using (var db = new MainContext(options))
            {
                db.Users.AddRange(visitor, owner);
                db.Rooms.AddRange(
                    new Room
                    {
                        Id = 9000,
                        OwnerCharacterId = 8101,
                        Name = "Visitor's Default Room",
                        Stage = MyRoomStage.SixTatami,
                        IsDefault = true,
                    },
                    new Room
                    {
                        Id = 9001,
                        OwnerCharacterId = 8102,
                        Name = "Owner's Twelve Tatami Room",
                        Stage = MyRoomStage.TwelveTatami,
                        IsDefault = true,
                    }
                );
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
                    new Map { MapId = 10990100, Name = "Akihabara" },
                    new Map
                    {
                        MapId = MyRoomInfo.EightTatamiMapId,
                        Name = "My Room (8 tatami mats)",
                    },
                    new Map
                    {
                        MapId = MyRoomInfo.TwelveTatamiMapId,
                        Name = "My Room (12 tatami mats)",
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var areaSession = new CapturingPlayerSession
            {
                User = visitor,
                UserId = visitor.Id,
                Character = visitor.Characters.First(),
                CharacterId = 8101,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, areaSession);

            var msgSession = new CapturingPlayerSession { User = visitor, UserId = visitor.Id };
            var roomRepository = new MyRoomRepository(new MainContext(options));
            var handler = new CmdExecHandler(
                state,
                new MapRepository(new MainContext(options)),
                new UserRepository(new MainContext(options)),
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                roomRepository,
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                NullLogger<CmdExecHandler>.Instance
            );

            await handler.HandleAsync(
                BuildCmdExecPayload("room", "9001"),
                msgSession,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(MyRoomInfo.TwelveTatamiMapId, areaSession.MapId);
            Assert.Equal(9001u, areaSession.MyRoomId);
            var notify = Assert.Single(
                areaSession.Sent,
                packet => packet.Type == PacketType.NotifyChangeMyRoom
            );
            var roomOffset = NotifyChangeMap.PacketSize - 1;
            var reader = new PacketReader(notify.Payload.AsSpan(roomOffset, 75));
            Assert.Equal(9001u, reader.ReadUInt());
            Assert.Equal(8102u, reader.ReadUInt());

            await using var verifyDb = new MainContext(options);
            var persistedVisitor = await verifyDb.Characters.SingleAsync(
                character => character.Id == 8101,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(9001, persistedVisitor.CurrentRoomId);

            areaSession.Sent.Clear();
            await handler.HandleAsync(
                BuildCmdExecPayload("room", "create", "8", "Second Room"),
                msgSession,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(MyRoomInfo.EightTatamiMapId, areaSession.MapId);
            var createdRoom = await verifyDb
                .Rooms.AsNoTracking()
                .Where(room => room.OwnerCharacterId == 8101 && !room.IsDefault)
                .SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal("Second Room", createdRoom.Name);
            Assert.Equal(MyRoomStage.EightTatami, createdRoom.Stage);
            Assert.Equal(checked((uint)createdRoom.Id), areaSession.MyRoomId);
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

            var handler = new CmdExecHandler(
                state,
                new MapRepository(new MainContext(options)),
                new UserRepository(new MainContext(options)),
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                new MyRoomRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                NullLogger<CmdExecHandler>.Instance
            );

            await handler.HandleAsync(
                BuildCmdExecPayload("jump"),
                msgSession,
                TestContext.Current.CancellationToken
            );

            // Rotation 90° faces +X, so a default jump moves along X.
            Assert.Equal(200f, areaSession.X, precision: 3);
            Assert.Equal(2f, areaSession.Y, precision: 3);
            Assert.Equal(200f, areaSession.Z, precision: 3);

            await handler.HandleAsync(
                BuildCmdExecPayload("jump", "50"),
                msgSession,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(250f, areaSession.X, precision: 3);
            Assert.Equal(200f, areaSession.Z, precision: 3);
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

            var handler = new CmdExecHandler(
                state,
                new MapRepository(new MainContext(options)),
                new UserRepository(new MainContext(options)),
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                new MyRoomRepository(new MainContext(options)),
                new StubItemBaseListCache(DefaultClothingItems.Male),
                CreateDirectMapLinkTransitionService(options, state),
                NullLogger<CmdExecHandler>.Instance
            );

            await handler.HandleAsync(
                BuildCmdExecPayload("outfit"),
                msgSession,
                TestContext.Current.CancellationToken
            );

            await using var verifyDb = new MainContext(options);
            var inventory = await verifyDb
                .CharacterInventories.Where(i => i.CharacterId == 8004)
                .ToListAsync(TestContext.Current.CancellationToken);
            var expectedItems = DefaultClothingItems.WardrobeInventoryForGender(1).ToList();
            Assert.Equal(expectedItems.Count, inventory.Count);
            Assert.All(
                expectedItems,
                itemId => Assert.Contains(inventory, i => i.ItemId == itemId && i.Quantity == 1)
            );
            Assert.Contains(areaSession.Sent, p => p.Type == PacketType.ItemGetListResponse);
            Assert.Equal(
                expectedItems.Count,
                areaSession.Sent.Count(p => p.Type == PacketType.ItemCreateNotify)
            );
            Assert.Equal(
                expectedItems.Count,
                areaSession.Sent.Count(p => p.Type == PacketType.ItemUpdateListNotify)
            );
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

            var handler = new CmdExecHandler(
                state,
                new MapRepository(new MainContext(options)),
                new UserRepository(new MainContext(options)),
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                new MyRoomRepository(new MainContext(options)),
                new StubItemBaseListCache([itemId]),
                CreateDirectMapLinkTransitionService(options, state),
                NullLogger<CmdExecHandler>.Instance
            );

            await handler.HandleAsync(
                BuildCmdExecPayload("/give", itemId.ToString()),
                msgSession,
                TestContext.Current.CancellationToken
            );

            await using var verifyDb = new MainContext(options);
            var inventory = await verifyDb.CharacterInventories.SingleAsync(
                i => i.CharacterId == 8005 && i.ItemId == itemId,
                TestContext.Current.CancellationToken
            );
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
            var user = CreateUserWithCharacter(
                1,
                8006,
                "give-missing-user",
                "Give Missing User",
                10990100
            );
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

            var handler = new CmdExecHandler(
                state,
                new MapRepository(new MainContext(options)),
                new UserRepository(new MainContext(options)),
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                new MyRoomRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                NullLogger<CmdExecHandler>.Instance
            );

            await handler.HandleAsync(
                BuildCmdExecPayload("/give", itemId.ToString()),
                msgSession,
                TestContext.Current.CancellationToken
            );

            await using var verifyDb = new MainContext(options);
            var inventoryCount = await verifyDb.CharacterInventories.CountAsync(
                i => i.CharacterId == 8006 && i.ItemId == itemId,
                TestContext.Current.CancellationToken
            );
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

            var handler = new CmdExecHandler(
                state,
                new MapRepository(new MainContext(options)),
                new UserRepository(new MainContext(options)),
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                new MyRoomRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                NullLogger<CmdExecHandler>.Instance
            );

            await handler.HandleAsync(
                BuildCmdExecPayload("/money", "50", "nico"),
                msgSession,
                TestContext.Current.CancellationToken
            );

            await using var verifyDb = new MainContext(options);
            var updated = await verifyDb.Users.SingleAsync(
                u => u.Id == user.Id,
                TestContext.Current.CancellationToken
            );
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
        var handler = new AreaEventScriptPlayHandler(
            NullLogger<AreaEventScriptPlayHandler>.Instance
        );

        var writer = new PacketWriter();
        writer.Write(0);
        await handler.HandleAsync(
            writer.ToBytes(),
            areaSession,
            TestContext.Current.CancellationToken
        );

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
            var handler = new AreaEventFadeInHandler(
                eventRepo,
                NullLogger<AreaEventFadeInHandler>.Instance
            );

            await handler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                areaSession,
                TestContext.Current.CancellationToken
            );

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
    public async Task PosCommand_SendsLocationAsSystemTalkForwardToMsgClient()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 8002, "pos-user", "Pos User", 10990100);
            var state = new SharedState();
            var areaSession = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
                Character = user.Characters.First(),
                CharacterId = 8002,
                MapId = 10990100,
                ChannelId = 1,
                X = -9055.5f,
                Y = 2f,
                Z = -17988.75f,
                Rotation = 180,
            };
            state.RegisterClient(ServerType.Area, areaSession);

            var msgSession = new CapturingPlayerSession { User = user, UserId = user.Id };
            var handler = new CmdExecHandler(
                state,
                new MapRepository(new MainContext(options)),
                new UserRepository(new MainContext(options)),
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                new MyRoomRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                NullLogger<CmdExecHandler>.Instance
            );

            await handler.HandleAsync(
                BuildCmdExecPayload("/pos"),
                msgSession,
                TestContext.Current.CancellationToken
            );

            Assert.Contains(msgSession.Sent, packet => packet.Type == PacketType.CmdExecResponse);
            var message = Assert.Single(
                msgSession.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
            var reader = new PacketReader(message.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(unchecked((uint)-5), reader.ReadUInt());
            var text = reader.ReadString("utf-8");
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Contains("Char: 8002", text);
            Assert.Contains("Map: 10990100", text);
            Assert.Contains("Ch: 1", text);
            Assert.Contains("X: -9055.5f", text);
            Assert.Contains("Y: 2f", text);
            Assert.Contains("Z: -17988.75f", text);
            Assert.Contains("Rot: 180", text);
        }
        finally
        {
            await connection.DisposeAsync();
        }
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

    private static User CreateUserWithCharacter(
        int userId,
        int characterId,
        string username,
        string characterName,
        uint currentMapId
    )
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

    private static DirectMapLinkTransitionService CreateDirectMapLinkTransitionService(
        DbContextOptions<MainContext> options,
        SharedState state
    )
    {
        return new DirectMapLinkTransitionService(
            new MapRepository(new MainContext(options)),
            new CharacterRepository(
                new MainContext(options),
                NullLogger<CharacterRepository>.Instance
            ),
            new MyRoomRepository(new MainContext(options)),
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

    private sealed class StubItemBaseListCache(IEnumerable<int>? itemIds = null)
        : IItemBaseListCache
    {
        private readonly HashSet<int> _itemIds = itemIds?.ToHashSet() ?? [];

        public ReadOnlyMemory<byte> ResponsePayload => ReadOnlyMemory<byte>.Empty;

        public Task WarmAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> ContainsItemAsync(int itemId, CancellationToken ct = default) =>
            Task.FromResult(_itemIds.Contains(itemId));
    }
}
