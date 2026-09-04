using aisp.Common.Config;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Handlers.Msg;
using aisp.Common.Localisation;
using aisp.Common.Services;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using aisp.Network.Packets.Msg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace aisp.Common.Tests;

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
                new CircleRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                CreateModerationService(options, state),
                new ChatLogRepository(new MainContext(options)),
                new ReportTicketRepository(new MainContext(options)),
                TestTextLocaliser.English,
                new AdventureWorkRepository(new MainContext(options)),
                WordFilter.FromTerms([]),
                new ScreenAssignments(),
                Options.Create(new ServerOptions()),
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

    [Fact]
    public async Task TpuCommand_TeleportsModeratorToTargetPlayerLocation()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var mod = CreateUserWithCharacter(1, 9001, "moduser", "ModChar", 10990100);
            mod.Role = UserRole.Moderator;
            var target = CreateUserWithCharacter(2, 8001, "target", "TargetChar", 10990110);

            await using (var db = new MainContext(options))
            {
                db.Users.AddRange(mod, target);
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
            var modAreaSession = new CapturingPlayerSession
            {
                User = mod,
                UserId = mod.Id,
                Character = mod.Characters.First(),
                CharacterId = 9001,
                MapId = 10990100,
                ChannelId = 1,
                X = -9100f,
                Y = 2f,
                Z = -18000f,
            };
            var targetAreaSession = new CapturingPlayerSession
            {
                User = target,
                UserId = target.Id,
                Character = target.Characters.First(),
                CharacterId = 8001,
                MapId = 10990110,
                ChannelId = 1,
                X = 123.5f,
                Y = 4.2f,
                Z = 456.7f,
                Rotation = 90,
            };
            state.RegisterClient(ServerType.Area, modAreaSession);
            state.RegisterClient(ServerType.Area, targetAreaSession);

            var modMsgSession = new CapturingPlayerSession
            {
                User = mod,
                UserId = mod.Id,
                Character = mod.Characters.First(),
                CharacterId = 9001,
            };

            var handler = new CmdExecHandler(
                state,
                new MapRepository(new MainContext(options)),
                new UserRepository(new MainContext(options)),
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                new MyRoomRepository(new MainContext(options)),
                new CircleRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                CreateModerationService(options, state),
                new ChatLogRepository(new MainContext(options)),
                new ReportTicketRepository(new MainContext(options)),
                TestTextLocaliser.English,
                new AdventureWorkRepository(new MainContext(options)),
                WordFilter.FromTerms([]),
                new ScreenAssignments(),
                Options.Create(new ServerOptions()),
                NullLogger<CmdExecHandler>.Instance
            );

            await handler.HandleAsync(
                BuildCmdExecPayload("/tpu", "2"),
                modMsgSession,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(10990110u, modAreaSession.MapId);
            Assert.Equal(123.5f, modAreaSession.X);
            Assert.Equal(4.2f, modAreaSession.Y);
            Assert.Equal(456.7f, modAreaSession.Z);
            Assert.Equal(90, modAreaSession.Rotation);
            Assert.Contains(
                modAreaSession.Sent,
                packet => packet.Type == PacketType.NotifyChangeMap
            );
            var notice = Assert.Single(
                modMsgSession.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
            var reader = new PacketReader(notice.Payload);
            reader.ReadUInt();
            reader.ReadUInt();
            Assert.Contains(
                "target",
                reader.ReadString("utf-8"),
                StringComparison.OrdinalIgnoreCase
            );
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
                new CircleRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                CreateModerationService(options, state),
                new ChatLogRepository(new MainContext(options)),
                new ReportTicketRepository(new MainContext(options)),
                TestTextLocaliser.English,
                new AdventureWorkRepository(new MainContext(options)),
                WordFilter.FromTerms([]),
                new ScreenAssignments(),
                Options.Create(new ServerOptions()),
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
                new CircleRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                CreateModerationService(options, state),
                new ChatLogRepository(new MainContext(options)),
                new ReportTicketRepository(new MainContext(options)),
                TestTextLocaliser.English,
                new AdventureWorkRepository(new MainContext(options)),
                WordFilter.FromTerms([]),
                new ScreenAssignments(),
                Options.Create(new ServerOptions()),
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
                        Security = MyRoomSecurity.Public,
                        IsDefault = true,
                    },
                    new Room
                    {
                        Id = 9002,
                        OwnerCharacterId = 8101,
                        Name = "Furnished Room",
                        Stage = MyRoomStage.TenTatami,
                    }
                );
                db.Items.Add(new Item { Id = 7001, Name = "Test Furniture" });
                db.Furniture.Add(new Furniture { ItemId = 7001 });
                db.MyRoomFurniture.Add(
                    new MyRoomFurniture
                    {
                        RoomId = 9002,
                        FurnitureId = 1,
                        ItemId = 7001,
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
                new CircleRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                CreateModerationService(options, state),
                new ChatLogRepository(new MainContext(options)),
                new ReportTicketRepository(new MainContext(options)),
                TestTextLocaliser.English,
                new AdventureWorkRepository(new MainContext(options)),
                WordFilter.FromTerms(["faggot"]),
                new ScreenAssignments(),
                Options.Create(new ServerOptions()),
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
                .Where(room => room.OwnerCharacterId == 8101 && room.Name == "Second Room")
                .SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal("Second Room", createdRoom.Name);
            Assert.Equal(MyRoomStage.EightTatami, createdRoom.Stage);
            Assert.Equal(checked((uint)createdRoom.Id), areaSession.MyRoomId);

            await handler.HandleAsync(
                BuildCmdExecPayload("room", "create", "8", "Faggot"),
                msgSession,
                TestContext.Current.CancellationToken
            );
            Assert.False(
                await verifyDb
                    .Rooms.AsNoTracking()
                    .AnyAsync(
                        room => room.OwnerCharacterId == 8101 && room.Name == "Faggot",
                        TestContext.Current.CancellationToken
                    )
            );

            msgSession.Sent.Clear();
            await handler.HandleAsync(
                BuildCmdExecPayload("room", "set"),
                msgSession,
                TestContext.Current.CancellationToken
            );

            var ownedRooms = await verifyDb
                .Rooms.AsNoTracking()
                .Where(room => room.OwnerCharacterId == 8101)
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.True(ownedRooms.Single(room => room.Id == createdRoom.Id).IsDefault);
            Assert.False(ownedRooms.Single(room => room.Id == 9000).IsDefault);
            var notice = Assert.Single(
                msgSession.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
            var noticeReader = new PacketReader(notice.Payload);
            Assert.Equal(0u, noticeReader.ReadUInt());
            Assert.Equal(unchecked((uint)-5), noticeReader.ReadUInt());
            Assert.Contains(
                "default room",
                noticeReader.ReadString("utf-8"),
                StringComparison.Ordinal
            );

            msgSession.Sent.Clear();
            await handler.HandleAsync(
                BuildCmdExecPayload("room", "list"),
                msgSession,
                TestContext.Current.CancellationToken
            );
            // A list longer than one notice allows arrives as several; read them all.
            var listText = string.Join(
                "\n",
                msgSession
                    .Sent.Where(packet => packet.Type == PacketType.TalkForwardNotify)
                    .Select(packet =>
                    {
                        var listReader = new PacketReader(packet.Payload);
                        listReader.ReadUInt();
                        listReader.ReadUInt();
                        return listReader.ReadString("utf-8");
                    })
            );
            Assert.Contains("9000: Visitor's Default Room", listText, StringComparison.Ordinal);
            Assert.Contains(
                $"{createdRoom.Id}: Second Room (8 tatami) [default]",
                listText,
                StringComparison.Ordinal
            );

            msgSession.Sent.Clear();
            await handler.HandleAsync(
                BuildCmdExecPayload("room", "remove", "9000"),
                msgSession,
                TestContext.Current.CancellationToken
            );
            Assert.False(
                await verifyDb
                    .Rooms.AsNoTracking()
                    .AnyAsync(room => room.Id == 9000, TestContext.Current.CancellationToken)
            );
            var removeNotice = Assert.Single(
                msgSession.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
            var removeReader = new PacketReader(removeNotice.Payload);
            removeReader.ReadUInt();
            removeReader.ReadUInt();
            Assert.Contains("was removed", removeReader.ReadString("utf-8"));

            msgSession.Sent.Clear();
            await handler.HandleAsync(
                BuildCmdExecPayload("room", "remove", "9002"),
                msgSession,
                TestContext.Current.CancellationToken
            );
            Assert.True(
                await verifyDb
                    .Rooms.AsNoTracking()
                    .AnyAsync(room => room.Id == 9002, TestContext.Current.CancellationToken)
            );
            var notEmptyNotice = Assert.Single(
                msgSession.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
            var notEmptyReader = new PacketReader(notEmptyNotice.Payload);
            notEmptyReader.ReadUInt();
            notEmptyReader.ReadUInt();
            Assert.Contains("containing furniture", notEmptyReader.ReadString("utf-8"));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task RoomCommand_PrivateRoom_SendsSystemNoticeAndDoesNotTeleport()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var visitor = CreateUserWithCharacter(
                1,
                8201,
                "private-visitor",
                "Private Visitor",
                10990100
            );
            visitor.Characters.First().HomeIslandId = 1;
            var owner = CreateUserWithCharacter(
                2,
                8202,
                "private-owner",
                "Private Owner",
                10990100
            );

            await using (var db = new MainContext(options))
            {
                db.Users.AddRange(visitor, owner);
                db.Rooms.Add(
                    new Room
                    {
                        Id = 9101,
                        OwnerCharacterId = 8202,
                        Name = "Private Room",
                        Stage = MyRoomStage.SixTatami,
                        Security = MyRoomSecurity.Private,
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
                    new Map { MapId = MyRoomInfo.BaseMapId, Name = "My Room (6 tatami mats)" }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var areaSession = new CapturingPlayerSession
            {
                User = visitor,
                UserId = visitor.Id,
                Character = visitor.Characters.First(),
                CharacterId = 8201,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, areaSession);

            var msgSession = new CapturingPlayerSession { User = visitor, UserId = visitor.Id };
            var handler = new CmdExecHandler(
                state,
                new MapRepository(new MainContext(options)),
                new UserRepository(new MainContext(options)),
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                new MyRoomRepository(new MainContext(options)),
                new CircleRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                CreateModerationService(options, state),
                new ChatLogRepository(new MainContext(options)),
                new ReportTicketRepository(new MainContext(options)),
                TestTextLocaliser.English,
                new AdventureWorkRepository(new MainContext(options)),
                WordFilter.FromTerms([]),
                new ScreenAssignments(),
                Options.Create(new ServerOptions()),
                NullLogger<CmdExecHandler>.Instance
            );

            await handler.HandleAsync(
                BuildCmdExecPayload("room", "9101"),
                msgSession,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(10990100u, areaSession.MapId);
            Assert.Equal(0u, areaSession.MyRoomId);
            var notice = Assert.Single(
                msgSession.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
            var reader = new PacketReader(notice.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(unchecked((uint)-5), reader.ReadUInt());
            Assert.Contains("Private", reader.ReadString("utf-8"), StringComparison.Ordinal);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task RoomCommand_MissingRoom_SendsSystemNotice()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var visitor = CreateUserWithCharacter(
                1,
                8301,
                "missing-room-visitor",
                "Missing Room Visitor",
                10990100
            );
            visitor.Characters.First().HomeIslandId = 1;

            await using (var db = new MainContext(options))
            {
                db.Users.Add(visitor);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var areaSession = new CapturingPlayerSession
            {
                User = visitor,
                UserId = visitor.Id,
                Character = visitor.Characters.First(),
                CharacterId = 8301,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, areaSession);

            var msgSession = new CapturingPlayerSession { User = visitor, UserId = visitor.Id };
            var handler = new CmdExecHandler(
                state,
                new MapRepository(new MainContext(options)),
                new UserRepository(new MainContext(options)),
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                new MyRoomRepository(new MainContext(options)),
                new CircleRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                CreateModerationService(options, state),
                new ChatLogRepository(new MainContext(options)),
                new ReportTicketRepository(new MainContext(options)),
                TestTextLocaliser.English,
                new AdventureWorkRepository(new MainContext(options)),
                WordFilter.FromTerms([]),
                new ScreenAssignments(),
                Options.Create(new ServerOptions()),
                NullLogger<CmdExecHandler>.Instance
            );

            await handler.HandleAsync(
                BuildCmdExecPayload("room", "999999"),
                msgSession,
                TestContext.Current.CancellationToken
            );

            var notice = Assert.Single(
                msgSession.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
            var reader = new PacketReader(notice.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(unchecked((uint)-5), reader.ReadUInt());
            Assert.Contains("does not exist", reader.ReadString("utf-8"), StringComparison.Ordinal);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("2147483648")]
    [InlineData("abc")]
    public async Task RoomCommand_InvalidRoomId_SendsSystemNotice(string roomIdArgument)
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var visitor = CreateUserWithCharacter(
                1,
                8401,
                "invalid-room-visitor",
                "Invalid Room Visitor",
                10990100
            );
            visitor.Characters.First().HomeIslandId = 1;

            await using (var db = new MainContext(options))
            {
                db.Users.Add(visitor);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var areaSession = new CapturingPlayerSession
            {
                User = visitor,
                UserId = visitor.Id,
                Character = visitor.Characters.First(),
                CharacterId = 8401,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, areaSession);

            var msgSession = new CapturingPlayerSession { User = visitor, UserId = visitor.Id };
            var handler = new CmdExecHandler(
                state,
                new MapRepository(new MainContext(options)),
                new UserRepository(new MainContext(options)),
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                new MyRoomRepository(new MainContext(options)),
                new CircleRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                CreateModerationService(options, state),
                new ChatLogRepository(new MainContext(options)),
                new ReportTicketRepository(new MainContext(options)),
                TestTextLocaliser.English,
                new AdventureWorkRepository(new MainContext(options)),
                WordFilter.FromTerms([]),
                new ScreenAssignments(),
                Options.Create(new ServerOptions()),
                NullLogger<CmdExecHandler>.Instance
            );

            await handler.HandleAsync(
                BuildCmdExecPayload("room", roomIdArgument),
                msgSession,
                TestContext.Current.CancellationToken
            );

            var notice = Assert.Single(
                msgSession.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
            var reader = new PacketReader(notice.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(unchecked((uint)-5), reader.ReadUInt());
            Assert.Contains(
                "Invalid room ID",
                reader.ReadString("utf-8"),
                StringComparison.Ordinal
            );
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
                new CircleRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                CreateModerationService(options, state),
                new ChatLogRepository(new MainContext(options)),
                new ReportTicketRepository(new MainContext(options)),
                TestTextLocaliser.English,
                new AdventureWorkRepository(new MainContext(options)),
                WordFilter.FromTerms([]),
                new ScreenAssignments(),
                Options.Create(new ServerOptions()),
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
                new CircleRepository(new MainContext(options)),
                new StubItemBaseListCache(DefaultClothingItems.Male),
                CreateDirectMapLinkTransitionService(options, state),
                CreateModerationService(options, state),
                new ChatLogRepository(new MainContext(options)),
                new ReportTicketRepository(new MainContext(options)),
                TestTextLocaliser.English,
                new AdventureWorkRepository(new MainContext(options)),
                WordFilter.FromTerms([]),
                new ScreenAssignments(),
                Options.Create(new ServerOptions()),
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
            Assert.DoesNotContain(
                areaSession.Sent,
                p => p.Type == PacketType.ItemUpdateListNotify
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
                new CircleRepository(new MainContext(options)),
                new StubItemBaseListCache([itemId]),
                CreateDirectMapLinkTransitionService(options, state),
                CreateModerationService(options, state),
                new ChatLogRepository(new MainContext(options)),
                new ReportTicketRepository(new MainContext(options)),
                TestTextLocaliser.English,
                new AdventureWorkRepository(new MainContext(options)),
                WordFilter.FromTerms([]),
                new ScreenAssignments(),
                Options.Create(new ServerOptions()),
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
            Assert.DoesNotContain(
                areaSession.Sent,
                p => p.Type == PacketType.ItemUpdateListNotify
            );
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
                new CircleRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                CreateModerationService(options, state),
                new ChatLogRepository(new MainContext(options)),
                new ReportTicketRepository(new MainContext(options)),
                TestTextLocaliser.English,
                new AdventureWorkRepository(new MainContext(options)),
                WordFilter.FromTerms([]),
                new ScreenAssignments(),
                Options.Create(new ServerOptions()),
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
                new CircleRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                CreateModerationService(options, state),
                new ChatLogRepository(new MainContext(options)),
                new ReportTicketRepository(new MainContext(options)),
                TestTextLocaliser.English,
                new AdventureWorkRepository(new MainContext(options)),
                WordFilter.FromTerms([]),
                new ScreenAssignments(),
                Options.Create(new ServerOptions()),
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
                new CircleRepository(new MainContext(options)),
                new StubItemBaseListCache(),
                CreateDirectMapLinkTransitionService(options, state),
                CreateModerationService(options, state),
                new ChatLogRepository(new MainContext(options)),
                new ReportTicketRepository(new MainContext(options)),
                TestTextLocaliser.English,
                new AdventureWorkRepository(new MainContext(options)),
                WordFilter.FromTerms([]),
                new ScreenAssignments(),
                Options.Create(new ServerOptions()),
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

    [Fact]
    public async Task ReportCommand_WithNoArgs_SendsUsageNotice()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var user = CreateUserWithCharacter(1, 8001, "report-user", "Reporter", 10990100);
            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var msgSession = new CapturingPlayerSession { User = user, UserId = user.Id };
            var handler = CreateReportHandler(options, new SharedState());

            await handler.HandleAsync(
                BuildCmdExecPayload("/report"),
                msgSession,
                TestContext.Current.CancellationToken
            );

            var notice = Assert.Single(
                msgSession.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
            var reader = new PacketReader(notice.Payload);
            reader.ReadUInt();
            reader.ReadUInt();
            var text = reader.ReadString("utf-8");
            Assert.Contains("/report", text, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ReportCommand_WithoutAreaSession_SendsNotInMapNotice()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var user = CreateUserWithCharacter(1, 8001, "report-user", "Reporter", 10990100);
            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var msgSession = new CapturingPlayerSession { User = user, UserId = user.Id };
            var handler = CreateReportHandler(options, new SharedState());

            await handler.HandleAsync(
                BuildCmdExecPayload("/report", "some", "reason"),
                msgSession,
                TestContext.Current.CancellationToken
            );

            var notice = Assert.Single(
                msgSession.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
            var reader = new PacketReader(notice.Payload);
            reader.ReadUInt();
            reader.ReadUInt();
            var text = reader.ReadString("utf-8");
            Assert.Contains("map", text, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ReportCommand_WithAreaSession_PersistsTicketWithSnapshots()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var reporter = CreateUserWithCharacter(1, 8001, "reporter", "Reporter", 10990100);
            var other = CreateUserWithCharacter(2, 8002, "other", "Other", 10990100);
            await using (var db = new MainContext(options))
            {
                db.Users.AddRange(reporter, other);
                db.Maps.Add(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Akihabara",
                        SpawnX = 0,
                        SpawnY = 0,
                        SpawnZ = 0,
                    }
                );
                db.ChatMessages.Add(
                    new ChatMessage
                    {
                        Kind = ChatLogKind.Public,
                        UserId = other.Id,
                        CharacterId = 8002,
                        CharacterName = "Other",
                        Message = "offensive chat",
                        MapId = 10990100,
                        ChannelId = 1,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var areaSession = new CapturingPlayerSession
            {
                User = reporter,
                UserId = reporter.Id,
                Character = reporter.Characters.First(),
                CharacterId = 8001,
                MapId = 10990100,
                ChannelId = 1,
            };
            var otherAreaSession = new CapturingPlayerSession
            {
                User = other,
                UserId = other.Id,
                Character = other.Characters.First(),
                CharacterId = 8002,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, areaSession);
            state.RegisterClient(ServerType.Area, otherAreaSession);

            var msgSession = new CapturingPlayerSession { User = reporter, UserId = reporter.Id };
            var handler = CreateReportHandler(options, state);

            await handler.HandleAsync(
                BuildCmdExecPayload("/report", "Bob", "is", "being", "racist"),
                msgSession,
                TestContext.Current.CancellationToken
            );

            var notice = Assert.Single(
                msgSession.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
            var reader = new PacketReader(notice.Payload);
            reader.ReadUInt();
            reader.ReadUInt();
            var text = reader.ReadString("utf-8");
            Assert.Contains("submitted", text, StringComparison.OrdinalIgnoreCase);

            await using var verifyDb = new MainContext(options);
            var ticket = Assert.Single(verifyDb.ReportTickets);
            Assert.Equal("Bob is being racist", ticket.Reason);
            Assert.Equal("Akihabara", ticket.MapName);
            Assert.Equal(2, verifyDb.ReportTicketPlayers.Count(x => x.ReportTicketId == ticket.Id));
            Assert.Single(
                verifyDb.ReportTicketChatMessages.Where(x => x.ReportTicketId == ticket.Id)
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ReportCommand_NotifiesModeratorsCircleChat()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var admin = CreateUserWithCharacter(1, 9001, "sysadmin", "AdminChar", 10990100);
            admin.Role = UserRole.ServerAdmin;
            var mod = CreateUserWithCharacter(2, 9002, "moduser", "ModChar", 10990100);
            mod.Role = UserRole.Moderator;
            var reporter = CreateUserWithCharacter(3, 8001, "reporter", "Reporter", 10990100);

            await using (var db = new MainContext(options))
            {
                db.Users.AddRange(admin, mod, reporter);
                db.Maps.Add(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Akihabara",
                        SpawnX = 0,
                        SpawnY = 0,
                        SpawnZ = 0,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            await CreateModerationService(options, state)
                .SyncAllStaffCirclesAsync(TestContext.Current.CancellationToken);

            var modMsgSession = new CapturingPlayerSession
            {
                User = mod,
                UserId = mod.Id,
                Character = mod.Characters.First(),
                CharacterId = 9002,
            };
            state.RegisterClient(ServerType.Msg, modMsgSession);

            var areaSession = new CapturingPlayerSession
            {
                User = reporter,
                UserId = reporter.Id,
                Character = reporter.Characters.First(),
                CharacterId = 8001,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, areaSession);

            var msgSession = new CapturingPlayerSession { User = reporter, UserId = reporter.Id };
            var handler = CreateReportHandler(options, state);

            await handler.HandleAsync(
                BuildCmdExecPayload("/report", "some", "problem"),
                msgSession,
                TestContext.Current.CancellationToken
            );

            var notify = Assert.Single(
                modMsgSession.Sent,
                packet => packet.Type == PacketType.CircleChatForwardNotify
            );
            var reader = new PacketReader(notify.Payload);
            Assert.Equal(9001u, reader.ReadUInt());
            var text = reader.ReadString("utf-8");
            Assert.Contains("Reporter", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("some problem", text, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UserListCommand_ListsOnlineUsersForModerators()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var mod = CreateUserWithCharacter(1, 9001, "moduser", "ModChar", 10990100);
            mod.Role = UserRole.Moderator;
            var player = CreateUserWithCharacter(2, 8001, "player", "PlayerChar", 10990100);
            var other = CreateUserWithCharacter(3, 8002, "other", "OtherChar", 10990100);

            await using (var db = new MainContext(options))
            {
                db.Users.AddRange(mod, player, other);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var modSession = new CapturingPlayerSession
            {
                User = mod,
                UserId = mod.Id,
                Character = mod.Characters.First(),
                CharacterId = 9001,
            };
            state.RegisterClient(ServerType.Msg, modSession);
            state.RegisterClient(
                ServerType.Msg,
                new CapturingPlayerSession
                {
                    User = player,
                    UserId = player.Id,
                    Character = player.Characters.First(),
                    CharacterId = 8001,
                }
            );
            state.RegisterClient(
                ServerType.Area,
                new CapturingPlayerSession
                {
                    User = other,
                    UserId = other.Id,
                    Character = other.Characters.First(),
                    CharacterId = 8002,
                    MapId = 10990100,
                    ChannelId = 1,
                }
            );

            var handler = CreateReportHandler(options, state);
            await handler.HandleAsync(
                BuildCmdExecPayload("/userlist"),
                modSession,
                TestContext.Current.CancellationToken
            );

            var notice = Assert.Single(
                modSession.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
            var reader = new PacketReader(notice.Payload);
            reader.ReadUInt();
            reader.ReadUInt();
            var text = reader.ReadString("utf-8");
            Assert.Contains("1: moduser", text, StringComparison.Ordinal);
            Assert.Contains("2: player", text, StringComparison.Ordinal);
            Assert.Contains("3: other", text, StringComparison.Ordinal);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UserListCommand_DeniesNonModerators()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var player = CreateUserWithCharacter(2, 8001, "player", "PlayerChar", 10990100);
            await using (var db = new MainContext(options))
            {
                db.Users.Add(player);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var session = new CapturingPlayerSession
            {
                User = player,
                UserId = player.Id,
                Character = player.Characters.First(),
                CharacterId = 8001,
            };
            state.RegisterClient(ServerType.Msg, session);

            var handler = CreateReportHandler(options, state);
            await handler.HandleAsync(
                BuildCmdExecPayload("/userlist"),
                session,
                TestContext.Current.CancellationToken
            );

            var notice = Assert.Single(
                session.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
            var reader = new PacketReader(notice.Payload);
            reader.ReadUInt();
            reader.ReadUInt();
            Assert.Contains(
                "permission",
                reader.ReadString("utf-8"),
                StringComparison.OrdinalIgnoreCase
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task TpuCommand_DeniesWhenPersistedRoleWasDemoted()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var demoted = CreateUserWithCharacter(1, 9001, "exmod", "ExMod", 10990100);
            demoted.Role = UserRole.User;
            var target = CreateUserWithCharacter(2, 8001, "target", "TargetChar", 10990110);

            await using (var db = new MainContext(options))
            {
                db.Users.AddRange(demoted, target);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var staleSessionUser = CreateUserWithCharacter(1, 9001, "exmod", "ExMod", 10990100);
            staleSessionUser.Role = UserRole.Moderator;

            var handler = CreateReportHandler(options, new SharedState());
            var session = new CapturingPlayerSession
            {
                User = staleSessionUser,
                UserId = demoted.Id,
                Character = staleSessionUser.Characters.First(),
                CharacterId = 9001,
            };

            await handler.HandleAsync(
                BuildCmdExecPayload("/tpu", "2"),
                session,
                TestContext.Current.CancellationToken
            );

            var notice = Assert.Single(
                session.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
            var reader = new PacketReader(notice.Payload);
            reader.ReadUInt();
            reader.ReadUInt();
            Assert.Contains(
                "permission",
                reader.ReadString("utf-8"),
                StringComparison.OrdinalIgnoreCase
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UserListCommand_AllowsWhenPersistedRoleWasPromoted()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var promoted = CreateUserWithCharacter(1, 9001, "newmod", "NewMod", 10990100);
            promoted.Role = UserRole.Moderator;
            var player = CreateUserWithCharacter(2, 8001, "player", "PlayerChar", 10990100);

            await using (var db = new MainContext(options))
            {
                db.Users.AddRange(promoted, player);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var staleSessionUser = CreateUserWithCharacter(1, 9001, "newmod", "NewMod", 10990100);
            staleSessionUser.Role = UserRole.User;

            var state = new SharedState();
            var session = new CapturingPlayerSession
            {
                User = staleSessionUser,
                UserId = promoted.Id,
                Character = staleSessionUser.Characters.First(),
                CharacterId = 9001,
            };
            state.RegisterClient(ServerType.Msg, session);
            state.RegisterClient(
                ServerType.Msg,
                new CapturingPlayerSession
                {
                    User = player,
                    UserId = player.Id,
                    Character = player.Characters.First(),
                    CharacterId = 8001,
                }
            );

            var handler = CreateReportHandler(options, state);
            await handler.HandleAsync(
                BuildCmdExecPayload("/userlist"),
                session,
                TestContext.Current.CancellationToken
            );

            var notice = Assert.Single(
                session.Sent,
                packet => packet.Type == PacketType.TalkForwardNotify
            );
            var reader = new PacketReader(notice.Payload);
            reader.ReadUInt();
            reader.ReadUInt();
            var text = reader.ReadString("utf-8");
            Assert.DoesNotContain("permission", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("1: newmod", text, StringComparison.Ordinal);
            Assert.Contains("2: player", text, StringComparison.Ordinal);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static CmdExecHandler CreateReportHandler(
        DbContextOptions<MainContext> options,
        SharedState state
    ) =>
        new(
            state,
            new MapRepository(new MainContext(options)),
            new UserRepository(new MainContext(options)),
            new CharacterRepository(
                new MainContext(options),
                NullLogger<CharacterRepository>.Instance
            ),
            new MyRoomRepository(new MainContext(options)),
            new CircleRepository(new MainContext(options)),
            new StubItemBaseListCache(),
            CreateDirectMapLinkTransitionService(options, state),
            CreateModerationService(options, state),
            new ChatLogRepository(new MainContext(options)),
            new ReportTicketRepository(new MainContext(options)),
            TestTextLocaliser.English,
            new AdventureWorkRepository(new MainContext(options)),
            WordFilter.FromTerms([]),
            new ScreenAssignments(),
            Options.Create(new ServerOptions()),
            NullLogger<CmdExecHandler>.Instance
        );

    private static byte[] BuildCmdExecPayload(string command, params string[] args)
    {
        var writer = new PacketWriter();
        writer.Write(1u);
        writer.WriteFixedString(command, 96);
        for (var i = 0; i < 10; i++)
            writer.WriteFixedString(i < args.Length ? args[i] : string.Empty, 384);
        writer.Write((uint)args.Length);
        return writer.ToBytes();
    }

    private static ModerationService CreateModerationService(
        DbContextOptions<MainContext> options,
        SharedState state
    ) =>
        new(
            new UserRepository(new MainContext(options)),
            new CharacterRepository(
                new MainContext(options),
                NullLogger<CharacterRepository>.Instance
            ),
            new CircleRepository(new MainContext(options)),
            new MainContext(options),
            state,
            NullLogger<ModerationService>.Instance
        );

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
            new CircleRepository(new MainContext(options)),
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
            TestTextLocaliser.English,
            NullLogger<DirectMapLinkTransitionService>.Instance
        );
    }

    private sealed class StubItemBaseListCache(IEnumerable<int>? itemIds = null)
        : IItemBaseListCache
    {
        private readonly HashSet<int> _itemIds = itemIds?.ToHashSet() ?? [];

        public ReadOnlyMemory<byte> GetResponsePayload(GameLanguage language) =>
            ReadOnlyMemory<byte>.Empty;

        public Task WarmAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> ContainsItemAsync(int itemId, CancellationToken ct = default) =>
            Task.FromResult(_itemIds.Contains(itemId));
    }
}
