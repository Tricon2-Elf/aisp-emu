using aisp.Common.Config;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Game.ServerScripts;
using aisp.Common.Handlers.Area;
using aisp.Common.Services;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using aisp.Network.Packets.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace aisp.Common.Tests;

public class AreaMapHandlersTests
{
    [Fact]
    public async Task MapLinkGetDataHandler_SendsPhysicalLinksInOrder_AndOnlyDirectRoutesInNotifySelectMap()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            await using (var db = new MainContext(options))
            {
                db.MapLinks.AddRange(
                    new MapLink
                    {
                        SourceMapId = 10990100,
                        ChannelId = 1,
                        PositionX = 10f,
                        PositionY = 1f,
                        PositionZ = 20f,
                        Yaw = 10,
                        Length = 100f,
                        Depth = 200f,
                        DestinationMapIds = "10990110",
                        SortOrder = 10,
                    },
                    new MapLink
                    {
                        SourceMapId = 10990100,
                        ChannelId = 1,
                        PositionX = 30f,
                        PositionY = 1f,
                        PositionZ = 40f,
                        Yaw = 30,
                        Length = 300f,
                        Depth = 400f,
                        DestinationMapIds = "10990110,10990200",
                        Behavior = MapLinkBehavior.ForceSelection,
                        SortOrder = 20,
                    },
                    new MapLink
                    {
                        SourceMapId = 10990100,
                        ChannelId = 1,
                        PositionX = 50f,
                        PositionY = 2f,
                        PositionZ = 60f,
                        Yaw = 50,
                        Length = 500f,
                        Depth = 600f,
                        DestinationMapIds = "10990210",
                        SortOrder = 30,
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
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination One",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 180,
                    },
                    new Map
                    {
                        MapId = 10990210,
                        Name = "Destination Two",
                        SpawnX = -9600f,
                        SpawnY = 0.1f,
                        SpawnZ = -8400f,
                        SpawnRotation = 90,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var session = CreateSession(
                CreateUserWithCharacter(1, 1001, "link-user", "Link Tester", 10990100),
                10990100,
                0
            );
            var logger = new ListLogger<AreaMapLinkGetDataHandler>();
            await using var runDb = new MainContext(options);
            var handler = new AreaMapLinkGetDataHandler(
                new MapLinkRepository(runDb),
                new MapRepository(runDb),
                new ChannelRepository(runDb),
                Options.Create(
                    new ServerOptions
                    {
                        NetworkOptions = new NetworkOptions(),
                        DbOptions = new DbOptions(),
                        IPOverride = "localhost",
                    }
                ),
                logger
            );

            await handler.HandleAsync(
                BuildUIntPairPayload(10990100, 1),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                session.Sent,
                packet => Assert.Equal(PacketType.MapLinkGetDataResponse, packet.Type),
                packet => Assert.Equal(PacketType.MapLinkNotifyData, packet.Type),
                packet => Assert.Equal(PacketType.MapLinkNotifyData, packet.Type),
                packet => Assert.Equal(PacketType.MapLinkNotifyData, packet.Type),
                packet => Assert.Equal(PacketType.NotifySelectMap, packet.Type)
            );

            var firstLink = OutgoingPacketTestParsers.ParseMapLinkNotifyData(
                session.Sent[1].Payload
            );
            Assert.Equal(10f, firstLink.Data.PositionX);
            Assert.Equal(20f, firstLink.Data.PositionZ);
            Assert.Equal(10, firstLink.Data.Yaw);

            var selectorLink = OutgoingPacketTestParsers.ParseMapLinkNotifyData(
                session.Sent[2].Payload
            );
            Assert.Equal(30f, selectorLink.Data.PositionX);
            Assert.Equal(40f, selectorLink.Data.PositionZ);
            Assert.Equal(30, selectorLink.Data.Yaw);

            var thirdLink = OutgoingPacketTestParsers.ParseMapLinkNotifyData(
                session.Sent[3].Payload
            );
            Assert.Equal(50f, thirdLink.Data.PositionX);
            Assert.Equal(60f, thirdLink.Data.PositionZ);
            Assert.Equal(50, thirdLink.Data.Yaw);

            Assert.Collection(
                ReadSelectMapEntries(session.Sent[4].Payload),
                entry =>
                {
                    Assert.Equal(10990110u, entry.MapId);
                    Assert.Equal((ushort)50054, entry.AreaServerPort);
                    Assert.Equal("localhost", entry.AreaServerIp);
                    Assert.Equal(1u, entry.ChannelId);
                    Assert.Equal(10990110u, entry.RouteMapId);
                    Assert.Equal(10990110u, entry.MapSerialId);
                    Assert.Equal(0u, entry.RouteState);
                    Assert.Equal(-11000f, entry.PositionX);
                    Assert.Equal(0.1f, entry.PositionY);
                    Assert.Equal(-19200f, entry.PositionZ);
                    Assert.Equal(180, entry.Yaw);
                    Assert.Equal((byte)0, entry.Animation);
                },
                entry =>
                {
                    Assert.Equal(10990210u, entry.MapId);
                    Assert.Equal((ushort)50054, entry.AreaServerPort);
                    Assert.Equal("localhost", entry.AreaServerIp);
                    Assert.Equal(1u, entry.ChannelId);
                    Assert.Equal(10990210u, entry.RouteMapId);
                    Assert.Equal(10990210u, entry.MapSerialId);
                    Assert.Equal(0u, entry.RouteState);
                    Assert.Equal(-9600f, entry.PositionX);
                    Assert.Equal(0.1f, entry.PositionY);
                    Assert.Equal(-8400f, entry.PositionZ);
                    Assert.Equal(90, entry.Yaw);
                    Assert.Equal((byte)0, entry.Animation);
                }
            );
            Assert.Equal(10990100u, session.MapId);
            Assert.Equal(1, session.ChannelId);
            Assert.DoesNotContain(
                logger.Entries,
                entry =>
                    entry.Level == LogLevel.Warning
                    && entry.Message.Contains("requires exactly one destination")
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task MapLinkGetDataHandler_UsesMapLinkDestinationSpawn_WhenConfigured()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            await using (var db = new MainContext(options))
            {
                db.MapLinks.Add(
                    new MapLink
                    {
                        SourceMapId = 10990100,
                        ChannelId = 1,
                        PositionX = 10f,
                        PositionY = 1f,
                        PositionZ = 20f,
                        Yaw = 10,
                        Length = 100f,
                        Depth = 200f,
                        DestinationMapIds = "10990110",
                        DestinationSpawnX = -8500f,
                        DestinationSpawnY = 2f,
                        DestinationSpawnZ = -15850f,
                        DestinationSpawnRotation = 180,
                        SortOrder = 10,
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
                db.Maps.Add(
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 180,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var session = CreateSession(
                CreateUserWithCharacter(1, 1002, "link-spawn-user", "Link Spawn", 10990100),
                10990100,
                0
            );
            await using var runDb = new MainContext(options);
            var handler = new AreaMapLinkGetDataHandler(
                new MapLinkRepository(runDb),
                new MapRepository(runDb),
                new ChannelRepository(runDb),
                Options.Create(
                    new ServerOptions
                    {
                        NetworkOptions = new NetworkOptions(),
                        DbOptions = new DbOptions(),
                        IPOverride = "localhost",
                    }
                ),
                NullLogger<AreaMapLinkGetDataHandler>.Instance
            );

            await handler.HandleAsync(
                BuildUIntPairPayload(10990100, 1),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                session.Sent,
                packet => Assert.Equal(PacketType.MapLinkGetDataResponse, packet.Type),
                packet => Assert.Equal(PacketType.MapLinkNotifyData, packet.Type),
                packet => Assert.Equal(PacketType.NotifySelectMap, packet.Type)
            );

            Assert.Collection(
                ReadSelectMapEntries(session.Sent[2].Payload),
                entry =>
                {
                    Assert.Equal(10990110u, entry.MapId);
                    Assert.Equal(-8500f, entry.PositionX);
                    Assert.Equal(2f, entry.PositionY);
                    Assert.Equal(-15850f, entry.PositionZ);
                    Assert.Equal(180, entry.Yaw);
                }
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task MapEnterHandler_UpdatesMapState_PersistsCharacterMap_AndNotifiesOldPeers()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 2001, "enter-user", "Traveler", 10990100);

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Source",
                        SpawnX = 1f,
                        SpawnY = 2f,
                        SpawnZ = 3f,
                        SpawnRotation = 8,
                    },
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination",
                        SpawnX = 11f,
                        SpawnY = 12f,
                        SpawnZ = 13f,
                        SpawnRotation = 28,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var session = CreateSession(user, 10990100, 1, x: 101f, y: 5f, z: 202f, rotation: 8);
            var oldPeer = CreateSession(
                CreateUserWithCharacter(2, 2002, "old-peer", "Old Peer", 10990100),
                10990100,
                1
            );
            var differentChannelPeer = CreateSession(
                CreateUserWithCharacter(3, 2003, "other-channel", "Other Channel", 10990100),
                10990100,
                2
            );
            var destinationPeer = CreateSession(
                CreateUserWithCharacter(4, 2004, "dest-peer", "Dest Peer", 10990110),
                10990110,
                1
            );

            state.RegisterClient(ServerType.Area, session);
            state.RegisterClient(ServerType.Area, oldPeer);
            state.RegisterClient(ServerType.Area, differentChannelPeer);
            state.RegisterClient(ServerType.Area, destinationPeer);

            await using var runDb = new MainContext(options);
            var handler = new AreaMapEnterHandler(
                new MapRepository(runDb),
                CreateDirectMapLinkTransitionService(runDb, state),
                state,
                NullLogger<AreaMapEnterHandler>.Instance
            );

            await handler.HandleAsync(
                BuildUIntPairPayload(10990110, 1),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Single(session.Sent);
            Assert.Equal(PacketType.MapEnterResponse, session.Sent[0].Type);
            Assert.Equal(0u, new PacketReader(session.Sent[0].Payload).ReadUInt());
            Assert.Equal(10990110u, session.MapId);
            Assert.Equal(1, session.ChannelId);
            Assert.Equal(11f, session.X);
            Assert.Equal(12f, session.Y);
            Assert.Equal(13f, session.Z);
            Assert.Equal(28, session.Rotation);
            Assert.Equal(10990110u, session.Character!.CurrentMapId);

            Assert.Collection(
                oldPeer.Sent,
                packet => Assert.Equal(PacketType.NotifyDisappearChara, packet.Type)
            );
            Assert.Empty(differentChannelPeer.Sent);
            Assert.Empty(destinationPeer.Sent);

            await using var verifyDb = new MainContext(options);
            var persistedCharacter = await verifyDb.Characters.SingleAsync(
                c => c.Id == session.Character.Id,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(10990110u, persistedCharacter.CurrentMapId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task MapEnterHandler_CurrentMapRequest_StandingInsideMapLink_DoesNotTransition()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 7001, "travel-user", "Traveler", 10990100);

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Source",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 180,
                    },
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
                    }
                );
                db.MapLinks.Add(
                    new MapLink
                    {
                        SourceMapId = 10990100,
                        ChannelId = 1,
                        PositionX = -9100f,
                        PositionY = 2f,
                        PositionZ = -18000f,
                        Yaw = 0,
                        Length = 1000f,
                        Depth = 1000f,
                        DestinationMapIds = "10990110",
                        SortOrder = 10,
                        IsEnabled = true,
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
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var session = CreateSession(
                user,
                10990100,
                1,
                x: -9100f,
                y: 2f,
                z: -17500f,
                rotation: 0
            );
            session.HasMovedSinceMapLoad = true;

            state.RegisterClient(ServerType.Area, session);

            await using var runDb = new MainContext(options);
            var handler = new AreaMapEnterHandler(
                new MapRepository(runDb),
                CreateDirectMapLinkTransitionService(runDb, state),
                state,
                NullLogger<AreaMapEnterHandler>.Instance
            );

            await handler.HandleAsync(
                BuildUIntPairPayload(10990100, 1),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Single(session.Sent);
            Assert.Equal(PacketType.MapEnterResponse, session.Sent[0].Type);
            Assert.Equal(10990100u, session.MapId);
            Assert.DoesNotContain(
                session.Sent,
                packet => packet.Type == PacketType.NotifyChangeMap
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AvatarMoveHandler_EnteringDirectMapLink_PushesNotifyChangeMap()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 7001, "travel-user", "Traveler", 10990100);

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Source",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 180,
                    },
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
                    }
                );
                db.MapLinks.Add(
                    new MapLink
                    {
                        SourceMapId = 10990100,
                        ChannelId = 1,
                        PositionX = -9100f,
                        PositionY = 2f,
                        PositionZ = -18000f,
                        Yaw = 0,
                        Length = 1000f,
                        Depth = 1000f,
                        DestinationMapIds = "10990110",
                        SortOrder = 10,
                        IsEnabled = true,
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
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            // Start outside the trigger (Z before the depth extent), then step inside.
            var session = CreateSession(
                user,
                10990100,
                1,
                x: -9100f,
                y: 2f,
                z: -18500f,
                rotation: 0
            );
            var oldPeer = CreateSession(
                CreateUserWithCharacter(2, 7002, "old-peer", "Old Peer", 10990100),
                10990100,
                1
            );

            state.RegisterClient(ServerType.Area, session);
            state.RegisterClient(ServerType.Area, oldPeer);

            await using var runDb = new MainContext(options);
            var handler = new AreaAvatarMoveRequestHandler(
                state,
                CreateDirectMapLinkTransitionService(runDb, state),
                CreateScriptedEventTriggerService(runDb)
            );

            await handler.HandleAsync(
                new MovementData(-9100f, 2f, -17500f, 0, MovementType.Running).ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                session.Sent,
                packet => Assert.Equal(PacketType.NotifyChangeMap, packet.Type)
            );

            var notify = OutgoingPacketTestParsers.ParseNotifyChangeMap(session.Sent[0].Payload);
            Assert.Equal(1u, notify.ChannelId);
            Assert.Equal(10990110u, notify.MapId);
            Assert.Equal(10990110u, notify.MapSerialId);
            Assert.Equal(-11000f, notify.PositionX);
            Assert.Equal(0.1f, notify.PositionY);
            Assert.Equal(-19200f, notify.PositionZ);
            Assert.Equal(0, notify.Rotation);
            Assert.Equal((byte)MovementType.Stopped, notify.Animation);
            Assert.Equal((byte)0, notify.Flag);
            Assert.Equal((ushort)0, notify.AreaServerInfo.Port);
            Assert.Equal(string.Empty, notify.AreaServerInfo.IP);
            Assert.Equal((byte)0, notify.FadeFlag);

            Assert.Equal(10990110u, session.MapId);
            Assert.Equal(1, session.ChannelId);
            Assert.Equal(-11000f, session.X);
            Assert.Equal(0.1f, session.Y);
            Assert.Equal(-19200f, session.Z);
            Assert.Equal(10990110u, session.Character!.CurrentMapId);

            Assert.Collection(
                oldPeer.Sent,
                packet => Assert.Equal(PacketType.NotifyDisappearChara, packet.Type)
            );
            Assert.False(state.TryTakePendingAreaTransition(user.Id, out _));
            Assert.True(session.NeedsPostLoadSelfAvatarNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AvatarMoveHandler_EnteringDirectMapLink_UsesMapLinkDestinationSpawn_WhenConfigured()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(
                1,
                7003,
                "custom-spawn-user",
                "Custom Spawn",
                10990100
            );

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Source",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 180,
                    },
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
                    }
                );
                db.MapLinks.Add(
                    new MapLink
                    {
                        SourceMapId = 10990100,
                        ChannelId = 1,
                        PositionX = -9100f,
                        PositionY = 2f,
                        PositionZ = -18000f,
                        Yaw = 0,
                        Length = 1000f,
                        Depth = 1000f,
                        DestinationMapIds = "10990110",
                        DestinationSpawnX = -8500f,
                        DestinationSpawnY = 2f,
                        DestinationSpawnZ = -15850f,
                        DestinationSpawnRotation = 180,
                        SortOrder = 10,
                        IsEnabled = true,
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
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var session = CreateSession(
                user,
                10990100,
                1,
                x: -9100f,
                y: 2f,
                z: -18500f,
                rotation: 0
            );

            state.RegisterClient(ServerType.Area, session);

            await using var runDb = new MainContext(options);
            var handler = new AreaAvatarMoveRequestHandler(
                state,
                CreateDirectMapLinkTransitionService(runDb, state),
                CreateScriptedEventTriggerService(runDb)
            );

            await handler.HandleAsync(
                new MovementData(-9100f, 2f, -17500f, 0, MovementType.Running).ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                session.Sent,
                packet => Assert.Equal(PacketType.NotifyChangeMap, packet.Type)
            );

            var notify = OutgoingPacketTestParsers.ParseNotifyChangeMap(session.Sent[0].Payload);
            Assert.Equal(10990110u, notify.MapId);
            Assert.Equal(-8500f, notify.PositionX);
            Assert.Equal(2f, notify.PositionY);
            Assert.Equal(-15850f, notify.PositionZ);
            Assert.Equal(180, notify.Rotation);

            Assert.Equal(10990110u, session.MapId);
            Assert.Equal(-8500f, session.X);
            Assert.Equal(2f, session.Y);
            Assert.Equal(-15850f, session.Z);
            Assert.Equal(180, session.Rotation);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AvatarMoveHandler_MovingWhileAlreadyInsideMapLink_DoesNotTransition()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(
                1,
                7004,
                "inside-move-user",
                "Inside Mover",
                10990100
            );

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Source",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 180,
                    },
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
                    }
                );
                db.MapLinks.Add(
                    new MapLink
                    {
                        SourceMapId = 10990100,
                        ChannelId = 1,
                        PositionX = -9100f,
                        PositionY = 2f,
                        PositionZ = -18000f,
                        Yaw = 0,
                        Length = 1000f,
                        Depth = 1000f,
                        DestinationMapIds = "10990110",
                        SortOrder = 10,
                        IsEnabled = true,
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
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            // Both start and end are inside the trigger volume.
            var session = CreateSession(
                user,
                10990100,
                1,
                x: -9100f,
                y: 2f,
                z: -17500f,
                rotation: 0
            );

            state.RegisterClient(ServerType.Area, session);

            await MarkIntroductionEventCompletedAsync(
                options,
                7004,
                TestContext.Current.CancellationToken
            );

            await using var runDb = new MainContext(options);
            var handler = new AreaAvatarMoveRequestHandler(
                state,
                CreateDirectMapLinkTransitionService(runDb, state),
                CreateScriptedEventTriggerService(runDb)
            );

            await handler.HandleAsync(
                new MovementData(-9000f, 2f, -17400f, 0, MovementType.Running).ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.DoesNotContain(
                session.Sent,
                packet => packet.Type == PacketType.NotifyChangeMap
            );
            Assert.Equal(10990100u, session.MapId);
            Assert.Equal(-9000f, session.X);
            Assert.Equal(-17400f, session.Z);
            Assert.True(session.HasMovedSinceMapLoad);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task MapEnterHandler_AcknowledgesPostNotifyChangeMapEnter_WithoutSecondNotifyChangeMap()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(
                1,
                7011,
                "post-notify-user",
                "Post Notify",
                10990110
            );

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Maps.Add(
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var session = CreateSession(
                user,
                10990110,
                1,
                x: -11000f,
                y: 0.1f,
                z: -19200f,
                rotation: 0
            );
            session.IsMapTransitionPending = true;
            session.NeedsPostLoadSelfAvatarNotify = true;

            state.RegisterClient(ServerType.Area, session);

            await using var runDb = new MainContext(options);
            var handler = new AreaMapEnterHandler(
                new MapRepository(runDb),
                CreateDirectMapLinkTransitionService(runDb, state),
                state,
                NullLogger<AreaMapEnterHandler>.Instance
            );

            await handler.HandleAsync(
                BuildUIntPairPayload(10990110, 1),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Single(session.Sent);
            Assert.Equal(PacketType.MapEnterResponse, session.Sent[0].Type);
            Assert.Equal(0u, new PacketReader(session.Sent[0].Payload).ReadUInt());
            Assert.True(session.IsMapTransitionPending);
            Assert.True(session.NeedsPostLoadSelfAvatarNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task DirectMapLinkTransitionService_SameAreaServer_DoesNotQueueAreasvEnterPendingTransition()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 7012, "same-area-user", "Same Area", 10990100);

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Source",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 180,
                    },
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
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
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var session = CreateSession(user, 10990100, 1);
            state.RegisterClient(ServerType.Area, session);

            await using var runDb = new MainContext(options);
            var service = CreateDirectMapLinkTransitionService(runDb, state);
            var destinationMap = await new MapRepository(runDb).GetByMapIdAsync(
                10990110,
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(destinationMap);

            var notify = await service.BuildNotifyChangeMapAsync(
                1,
                10990110,
                destinationMap,
                sourceChannelId: 1,
                TestContext.Current.CancellationToken
            );
            await service.CompleteMapTransitionAsync(
                session,
                session.Character!,
                10990110,
                1,
                destinationMap,
                notify,
                sendMapEnterResponse: false,
                TestContext.Current.CancellationToken
            );

            Assert.False(state.TryTakePendingAreaTransition(user.Id, out _));
            Assert.Equal((ushort)0, notify.AreaServerInfo.Port);
            Assert.Equal(string.Empty, notify.AreaServerInfo.IP);
            Assert.True(session.IsMapTransitionPending);
            Assert.True(session.NeedsPostLoadSelfAvatarNotify);
            Assert.Single(session.Sent);
            Assert.Equal(PacketType.NotifyChangeMap, session.Sent[0].Type);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task DirectMapLinkTransitionService_DifferentAreaServer_QueuesAreasvEnterPendingTransition()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(
                1,
                7013,
                "remote-area-user",
                "Remote Area",
                10990100
            );

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Source",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 180,
                    },
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
                    }
                );
                db.Channels.AddRange(
                    new GameChannel
                    {
                        ChannelNum = 1,
                        IP = "localhost",
                        Port = 50054,
                        MapId = 10990100,
                    },
                    new GameChannel
                    {
                        ChannelNum = 2,
                        IP = "remote-area",
                        Port = 50055,
                        MapId = 10990110,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var session = CreateSession(user, 10990100, 1);
            state.RegisterClient(ServerType.Area, session);

            await using var runDb = new MainContext(options);
            var service = CreateDirectMapLinkTransitionService(runDb, state);
            var destinationMap = await new MapRepository(runDb).GetByMapIdAsync(
                10990110,
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(destinationMap);

            var notify = await service.BuildNotifyChangeMapAsync(
                2,
                10990110,
                destinationMap,
                sourceChannelId: 1,
                TestContext.Current.CancellationToken
            );
            await service.CompleteMapTransitionAsync(
                session,
                session.Character!,
                10990110,
                2,
                destinationMap,
                notify,
                sendMapEnterResponse: false,
                TestContext.Current.CancellationToken
            );

            Assert.True(state.TryTakePendingAreaTransition(user.Id, out var pending));
            Assert.Equal((ushort)50055, notify.AreaServerInfo.Port);
            Assert.Equal("remote-area", notify.AreaServerInfo.IP);
            Assert.Equal(10990110u, pending.MapId);
            Assert.Equal(2, pending.ChannelId);
            Assert.Equal(-11000f, pending.X);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task MapEnterHandler_CurrentMapRequest_WithoutMovement_IsNoOpAck()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 7101, "idle-user", "Idle Traveler", 10990100);

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Maps.Add(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Source",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 180,
                    }
                );
                db.MapLinks.Add(
                    new MapLink
                    {
                        SourceMapId = 10990100,
                        ChannelId = 1,
                        PositionX = -9100f,
                        PositionY = 2f,
                        PositionZ = -18000f,
                        Yaw = 0,
                        Length = 1000f,
                        Depth = 1000f,
                        DestinationMapIds = "10990110",
                        SortOrder = 10,
                        IsEnabled = true,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var session = CreateSession(
                user,
                10990100,
                1,
                x: -9055f,
                y: 2f,
                z: -17988f,
                rotation: 0
            );
            session.HasMovedSinceMapLoad = false;

            state.RegisterClient(ServerType.Area, session);

            await using var runDb = new MainContext(options);
            var handler = new AreaMapEnterHandler(
                new MapRepository(runDb),
                CreateDirectMapLinkTransitionService(runDb, state),
                state,
                NullLogger<AreaMapEnterHandler>.Instance
            );

            await handler.HandleAsync(
                BuildUIntPairPayload(10990100, 1),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Single(session.Sent);
            Assert.Equal(PacketType.MapEnterResponse, session.Sent[0].Type);
            Assert.Equal(0u, new PacketReader(session.Sent[0].Payload).ReadUInt());
            Assert.Equal(10990100u, session.MapId);
            Assert.False(session.HasMovedSinceMapLoad);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AvatarMoveHandler_EnteringSelectorMapLink_OpensAreaMapSelector()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(
                1,
                7201,
                "selector-user",
                "Selector Traveler",
                10990100
            );

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Source",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 180,
                    },
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination One",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
                    },
                    new Map
                    {
                        MapId = 10990200,
                        Name = "Destination Two",
                        SpawnX = -9600f,
                        SpawnY = 0.1f,
                        SpawnZ = -8400f,
                        SpawnRotation = 0,
                    }
                );
                db.MapLinks.Add(
                    new MapLink
                    {
                        SourceMapId = 10990100,
                        ChannelId = 1,
                        PositionX = -9800f,
                        PositionY = 2f,
                        PositionZ = -18000f,
                        Yaw = 0,
                        Length = 300f,
                        Depth = 0f,
                        DestinationMapIds = "10990110,10990200",
                        Behavior = MapLinkBehavior.ForceSelection,
                        SortOrder = 10,
                        IsEnabled = true,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var session = CreateSession(
                user,
                10990100,
                1,
                x: -10450f,
                y: 2f,
                z: -18000f,
                rotation: 0
            );

            state.RegisterClient(ServerType.Area, session);

            await using var runDb = new MainContext(options);
            var handler = new AreaAvatarMoveRequestHandler(
                state,
                CreateDirectMapLinkTransitionService(runDb, state),
                CreateScriptedEventTriggerService(runDb)
            );

            await handler.HandleAsync(
                new MovementData(-9800f, 2f, -18000f, 0, MovementType.Running).ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                session.Sent,
                packet => Assert.Equal(PacketType.SelectInitIslandStart, packet.Type),
                packet => Assert.Equal(PacketType.EventAreaMapSelectExec, packet.Type)
            );

            var islandStart = OutgoingPacketTestParsers.ParseSelectInitIslandStartNotify(
                session.Sent[0].Payload
            );
            Assert.Collection(
                islandStart.Islands,
                island =>
                {
                    Assert.Equal(1u, island.IslandId);
                    Assert.Equal(
                        "Akihabara Station Front Street Event Area Island 1",
                        island.Title
                    );
                    Assert.Equal(
                        "Akihabara Station Front Street Event Area\nAkihabara UDX",
                        island.Description
                    );
                }
            );

            var selector = OutgoingPacketTestParsers.ParseEventAreaMapSelectExecNotify(
                session.Sent[1].Payload
            );
            Assert.Equal([10990110u, 10990200u], selector.MapIds);
            Assert.Equal(1u, selector.IslandId);
            Assert.Equal(0u, selector.IsRegisteredIsland);

            Assert.NotNull(session.PendingAreaMapSelection);
            Assert.Equal(2, session.PendingAreaMapSelection!.Destinations.Count);
            Assert.Equal(1u, session.PendingAreaMapSelection.IslandId);
            Assert.True(session.PendingAreaMapSelection.AwaitingIslandBootstrapAck);
            Assert.True(session.PendingAreaMapSelection.SelectorOpened);
            Assert.Equal(10990100u, session.MapId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AvatarMoveHandler_EnteringShuffleSelector_UsesFranchiseIslandIdNotAreaDigit()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(
                1,
                7301,
                "shuffle-selector-user",
                "Shuffle Selector",
                10030100
            );

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10030100,
                        Name = "Verbena Academy",
                        SpawnX = 10800f,
                        SpawnY = 0.1f,
                        SpawnZ = -1200f,
                        SpawnRotation = 0,
                    },
                    new Map
                    {
                        MapId = 10030200,
                        Name = "Shuffle Shopping Street",
                        SpawnX = 0f,
                        SpawnY = 0f,
                        SpawnZ = 0f,
                        SpawnRotation = 0,
                    },
                    new Map
                    {
                        MapId = 20000000,
                        Name = "My Room",
                        SpawnX = 0f,
                        SpawnY = 0f,
                        SpawnZ = 0f,
                        SpawnRotation = 0,
                    }
                );
                db.MapLinks.Add(
                    new MapLink
                    {
                        SourceMapId = 10030100,
                        ChannelId = 1,
                        PositionX = 11220f,
                        PositionY = 0f,
                        PositionZ = -10260f,
                        Yaw = 0,
                        Length = 100f,
                        Depth = 10f,
                        DestinationMapIds = "10030100,10030200,20000000",
                        Behavior = MapLinkBehavior.ForceSelection,
                        SortOrder = 10,
                        IsEnabled = true,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            // Start outside the depth extent (yaw 0 → +Z), then step into the volume.
            var session = CreateSession(
                user,
                10030100,
                1,
                x: 11220f,
                y: 0f,
                z: -10320f,
                rotation: 0
            );
            state.RegisterClient(ServerType.Area, session);

            await using var runDb = new MainContext(options);
            var handler = new AreaAvatarMoveRequestHandler(
                state,
                CreateDirectMapLinkTransitionService(runDb, state),
                CreateScriptedEventTriggerService(runDb)
            );

            await handler.HandleAsync(
                new MovementData(11220f, 0f, -10255f, 0, MovementType.Running).ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Contains(
                session.Sent,
                packet => packet.Type == PacketType.SelectInitIslandStart
            );
            Assert.Contains(
                session.Sent,
                packet => packet.Type == PacketType.EventAreaMapSelectExec
            );

            var islandStart = OutgoingPacketTestParsers.ParseSelectInitIslandStartNotify(
                session
                    .Sent.First(packet => packet.Type == PacketType.SelectInitIslandStart)
                    .Payload
            );
            Assert.Collection(islandStart.Islands, island => Assert.Equal(3u, island.IslandId));

            var selector = OutgoingPacketTestParsers.ParseEventAreaMapSelectExecNotify(
                session
                    .Sent.First(packet => packet.Type == PacketType.EventAreaMapSelectExec)
                    .Payload
            );
            Assert.Equal(3u, selector.IslandId);
            Assert.Equal(3u, session.PendingAreaMapSelection!.IslandId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task SelectInitIslandEndHandler_OpensPendingSelector_AfterIslandBootstrapAck()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            await using (var db = new MainContext(options))
            {
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination One",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
                    },
                    new Map
                    {
                        MapId = 10990200,
                        Name = "Destination Two",
                        SpawnX = -9600f,
                        SpawnY = 0.1f,
                        SpawnZ = -8400f,
                        SpawnRotation = 90,
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
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var session = new CapturingPlayerSession
            {
                UserId = 2,
                MapId = 10990100,
                ChannelId = 1,
                PendingAreaMapSelection = new PendingAreaMapSelection
                {
                    LinkId = 16,
                    SourceMapId = 10990100,
                    ChannelId = 1,
                    IslandId = 1,
                    IsRegisteredIsland = 0,
                    Destinations =
                    [
                        new AreaMapSelectionDestination(10990110, 1),
                        new AreaMapSelectionDestination(10990200, 1),
                    ],
                },
            };

            await using var runDb = new MainContext(options);
            var handler = new AreaSelectInitIslandEndHandler(
                CreateDirectMapLinkTransitionService(runDb, state),
                CreateServerScriptDispatcher(runDb),
                NullLogger<AreaSelectInitIslandEndHandler>.Instance
            );

            await handler.HandleAsync(
                OutgoingPacketTestParsers.SelectInitIslandEndRequestToBytes(
                    new SelectInitIslandEndRequest { IslandId = 1 }
                ),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                session.Sent,
                packet => Assert.Equal(PacketType.EventAreaMapSelectExec, packet.Type)
            );

            var selector = OutgoingPacketTestParsers.ParseEventAreaMapSelectExecNotify(
                session.Sent[0].Payload
            );
            Assert.Equal([10990110u, 10990200u], selector.MapIds);
            Assert.Equal(1u, selector.IslandId);
            Assert.Equal(0u, selector.IsRegisteredIsland);
            Assert.Collection(
                selector.Entries,
                entry =>
                {
                    Assert.Equal(10990110u, entry.MapId);
                    Assert.Equal(1u, entry.ChannelId);
                    Assert.Equal(10990110u, entry.RouteMapId);
                    Assert.Equal(10990110u, entry.MapSerialId);
                    Assert.Equal(-11000f, entry.PositionX);
                    Assert.Equal(0.1f, entry.PositionY);
                    Assert.Equal(-19200f, entry.PositionZ);
                },
                entry =>
                {
                    Assert.Equal(10990200u, entry.MapId);
                    Assert.Equal(1u, entry.ChannelId);
                    Assert.Equal(10990200u, entry.RouteMapId);
                    Assert.Equal(10990200u, entry.MapSerialId);
                    Assert.Equal(-9600f, entry.PositionX);
                    Assert.Equal(0.1f, entry.PositionY);
                    Assert.Equal(-8400f, entry.PositionZ);
                }
            );
            Assert.NotNull(session.PendingAreaMapSelection);
            Assert.False(session.PendingAreaMapSelection!.AwaitingIslandBootstrapAck);
            Assert.True(session.PendingAreaMapSelection.SelectorOpened);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task EventAreaMapSelectExecRHandler_TransitionsToSelectedDestination()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(
                1,
                7301,
                "selector-reply-user",
                "Selector Reply",
                10990100
            );

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Source",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 180,
                    },
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination One",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
                    },
                    new Map
                    {
                        MapId = 10990200,
                        Name = "Destination Two",
                        SpawnX = -9600f,
                        SpawnY = 0.1f,
                        SpawnZ = -8400f,
                        SpawnRotation = 90,
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
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var session = CreateSession(
                user,
                10990100,
                1,
                x: -9800f,
                y: 2f,
                z: -18000f,
                rotation: 0
            );
            session.PendingAreaMapSelection = new PendingAreaMapSelection
            {
                LinkId = 10,
                SourceMapId = 10990100,
                ChannelId = 1,
                IslandId = 1,
                IsRegisteredIsland = 0,
                Destinations =
                [
                    new AreaMapSelectionDestination(10990110, 1),
                    new AreaMapSelectionDestination(10990200, 1),
                ],
            };

            var oldPeer = CreateSession(
                CreateUserWithCharacter(2, 7302, "old-peer", "Old Peer", 10990100),
                10990100,
                1
            );
            state.RegisterClient(ServerType.Area, session);
            state.RegisterClient(ServerType.Area, oldPeer);

            await using var runDb = new MainContext(options);
            var handler = new AreaEventAreaMapSelectExecRHandler(
                CreateDirectMapLinkTransitionService(runDb, state),
                NullLogger<AreaEventAreaMapSelectExecRHandler>.Instance
            );

            await handler.HandleAsync(
                OutgoingPacketTestParsers.EventAreaMapSelectExecRRequestToBytes(
                    new EventAreaMapSelectExecRRequest
                    {
                        Result = 0,
                        MapId = 10990200,
                        ChannelId = 1,
                    }
                ),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                session.Sent,
                packet => Assert.Equal(PacketType.EventAreaMapSelectCloseNotify, packet.Type),
                packet => Assert.Equal(PacketType.NotifyChangeMap, packet.Type)
            );

            var close = OutgoingPacketTestParsers.ParseEventAreaMapSelectCloseNotify(
                session.Sent[0].Payload
            );
            Assert.Equal(0u, close.Result);

            var notify = OutgoingPacketTestParsers.ParseNotifyChangeMap(session.Sent[1].Payload);
            Assert.Equal(10990200u, notify.MapId);
            Assert.Equal(1u, notify.ChannelId);
            Assert.Equal((byte)0, notify.Flag);
            Assert.Equal((byte)0, notify.FadeFlag);

            Assert.Equal(10990200u, session.MapId);
            Assert.Equal(1, session.ChannelId);
            Assert.Equal(-9600f, session.X);
            Assert.Equal(0.1f, session.Y);
            Assert.Equal(-8400f, session.Z);
            Assert.Equal(90, session.Rotation);
            Assert.True(session.IsMapTransitionPending);
            Assert.Null(session.PendingAreaMapSelection);
            Assert.Collection(
                oldPeer.Sent,
                packet => Assert.Equal(PacketType.NotifyDisappearChara, packet.Type)
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task MapEnterHandler_SpawnsOnlyPeersInDestinationArea_OnPostLoadAck()
    {
        var state = new SharedState();
        var session = CreateSession(
            CreateUserWithCharacter(1, 3001, "spawn-user", "Spawn User", 10990110),
            10990110,
            1,
            x: 1f,
            y: 2f,
            z: 3f
        );
        session.HasMovedSinceMapLoad = false;
        var destinationPeer = CreateSession(
            CreateUserWithCharacter(2, 3002, "spawn-peer", "Spawn Peer", 10990110),
            10990110,
            1,
            x: 4f,
            y: 5f,
            z: 6f
        );
        var oldPeer = CreateSession(
            CreateUserWithCharacter(3, 3003, "old-peer", "Old Peer", 10990100),
            10990100,
            1
        );
        var differentChannelPeer = CreateSession(
            CreateUserWithCharacter(4, 3004, "other-channel", "Other Channel", 10990110),
            10990110,
            2
        );

        state.RegisterClient(ServerType.Area, session);
        state.RegisterClient(ServerType.Area, destinationPeer);
        state.RegisterClient(ServerType.Area, oldPeer);
        state.RegisterClient(ServerType.Area, differentChannelPeer);

        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var runDb = new MainContext(options);
            var handler = new AreaMapEnterHandler(
                new MapRepository(runDb),
                CreateDirectMapLinkTransitionService(runDb, state),
                state,
                NullLogger<AreaMapEnterHandler>.Instance
            );

            await handler.HandleAsync(
                BuildUIntPairPayload(10990110, 1),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                session.Sent,
                packet => Assert.Equal(PacketType.MapEnterResponse, packet.Type),
                packet =>
                {
                    Assert.Equal(PacketType.AvatarNotifyData, packet.Type);
                    Assert.Equal(1u, new PacketReader(packet.Payload).ReadUInt());
                }
            );
            Assert.Collection(
                destinationPeer.Sent,
                packet => Assert.Equal(PacketType.AvatarNotifyData, packet.Type)
            );
            Assert.Empty(oldPeer.Sent);
            Assert.Empty(differentChannelPeer.Sent);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task MapDataEnterEndHandler_SendsSelfAvatarNotify_EvenWithoutVisiblePeers()
    {
        var session = CreateSession(
            CreateUserWithCharacter(1, 3051, "solo-user", "Solo User", 10990110),
            10990110,
            1,
            x: 11f,
            y: 12f,
            z: 13f
        );
        session.NeedsPostLoadSelfAvatarNotify = true;

        var handler = new AreaMapDataEnterEndHandler(
            NullLogger<AreaMapDataEnterEndHandler>.Instance
        );

        await handler.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Collection(
            session.Sent,
            packet => Assert.Equal(PacketType.MapDataEnterEndResponse, packet.Type),
            packet => Assert.Equal(PacketType.MoneyUpdatedAipoint, packet.Type),
            packet => Assert.Equal(PacketType.MoneyUpdatedNicopoint, packet.Type),
            packet =>
            {
                Assert.Equal(PacketType.AvatarNotifyData, packet.Type);
                var reader = new PacketReader(packet.Payload);
                Assert.Equal(0u, reader.ReadUInt());
                var avatar = AvatarData.FromBytes(reader.ReadBytes(AvatarData.WireSize));
                Assert.Equal(1u, avatar.Character.Map.ChannelId);
                Assert.Equal(10990110u, avatar.Character.Map.MapId);
                Assert.Equal(10990110u, avatar.Character.Map.MapSerialId);
                Assert.Equal(11f, avatar.Character.Map.Movement.X);
                Assert.Equal(12f, avatar.Character.Map.Movement.Y);
                Assert.Equal(13f, avatar.Character.Map.Movement.Z);
            }
        );
    }

    [Fact]
    public async Task MapDataEnterEndHandler_DoesNotSpawnPeers()
    {
        var state = new SharedState();
        var session = CreateSession(
            CreateUserWithCharacter(1, 3061, "enter-end-user", "Enter End User", 10990110),
            10990110,
            1
        );
        session.NeedsPostLoadSelfAvatarNotify = false;
        var peer = CreateSession(
            CreateUserWithCharacter(2, 3062, "enter-end-peer", "Enter End Peer", 10990110),
            10990110,
            1
        );
        state.RegisterClient(ServerType.Area, session);
        state.RegisterClient(ServerType.Area, peer);

        var handler = new AreaMapDataEnterEndHandler(
            NullLogger<AreaMapDataEnterEndHandler>.Instance
        );

        await handler.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Collection(
            session.Sent,
            packet => Assert.Equal(PacketType.MapDataEnterEndResponse, packet.Type),
            packet => Assert.Equal(PacketType.MoneyUpdatedAipoint, packet.Type),
            packet => Assert.Equal(PacketType.MoneyUpdatedNicopoint, packet.Type)
        );
        Assert.Empty(peer.Sent);
    }

    [Fact]
    public async Task AreasvEnterHandler_FallsBackToMainChannelMap_WhenCurrentMapIdIsZero()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            const string otp = "main-channel-otp-12";
            var user = CreateUserWithCharacter(1, 3062, "fallback-user", "Fallback User", 0);

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.UserSessions.Add(
                    new UserSession
                    {
                        UserId = user.Id,
                        OTP = otp,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    }
                );
                db.Channels.Add(
                    new GameChannel
                    {
                        ChannelNum = 1,
                        IP = "localhost",
                        Port = 50054,
                        CurrentUsers = 0,
                        MaxUsers = 1000,
                        MapId = 10990100,
                    }
                );
                db.Maps.Add(
                    new Map
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

            var session = new CapturingPlayerSession();
            await using var handlerDb = new MainContext(options);
            var handler = new AreasvEnterHandler(
                new UserSessionRepository(handlerDb, NullLogger<UserSessionRepository>.Instance),
                new MapRepository(handlerDb),
                new ChannelRepository(handlerDb),
                new CharacterRepository(handlerDb, NullLogger<CharacterRepository>.Instance),
                new MyRoomRepository(handlerDb),
                new CircleRepository(handlerDb),
                new FriendRepository(handlerDb),
                new SharedState(),
                NullLogger<AreasvEnterHandler>.Instance
            );

            await handler.HandleAsync(
                BuildAreasvEnterPayload((uint)user.Id, otp),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(10990100u, session.MapId);
            Assert.Equal(1, session.ChannelId);
            Assert.Equal(10990100u, session.Character!.CurrentMapId);
            Assert.Equal(2f, session.Y);
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.AreasvEnterResponse);

            await using var verifyDb = new MainContext(options);
            var persisted = await verifyDb.Characters.SingleAsync(
                c => c.Id == 3062,
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
    public async Task AreasvEnterHandler_UsesPendingTransitionSpawn_WithoutRandomSpread()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            const string otp = "transition-otp-1234";
            var user = CreateUserWithCharacter(
                1,
                3061,
                "transition-user",
                "Transition User",
                10990110
            );

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.UserSessions.Add(
                    new UserSession
                    {
                        UserId = user.Id,
                        OTP = otp,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    }
                );
                db.Maps.Add(
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            state.SetPendingAreaTransition(
                new SharedState.PendingMapTransfer(user.Id, 10990110, 1, -11000f, 0.1f, -19200f, 0)
            );

            var session = new CapturingPlayerSession();
            await using var handlerDb = new MainContext(options);
            var handler = new AreasvEnterHandler(
                new UserSessionRepository(handlerDb, NullLogger<UserSessionRepository>.Instance),
                new MapRepository(handlerDb),
                new ChannelRepository(handlerDb),
                new CharacterRepository(handlerDb, NullLogger<CharacterRepository>.Instance),
                new MyRoomRepository(handlerDb),
                new CircleRepository(handlerDb),
                new FriendRepository(handlerDb),
                state,
                NullLogger<AreasvEnterHandler>.Instance
            );

            await handler.HandleAsync(
                BuildAreasvEnterPayload((uint)user.Id, otp),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(10990110u, session.MapId);
            Assert.Equal(1, session.ChannelId);
            Assert.Equal(-11000f, session.X);
            Assert.Equal(0.1f, session.Y);
            Assert.Equal(-19200f, session.Z);
            Assert.Equal(0, session.Rotation);
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.AreasvEnterResponse);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AvatarMoveHandler_ForwardsAllMoves_WithRunningAnimation()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var state = new SharedState();
            var mover = CreateSession(
                CreateUserWithCharacter(1, 4001, "move-user", "Mover", 10990100),
                10990100,
                1
            );
            var peer = CreateSession(
                CreateUserWithCharacter(2, 4002, "same-peer", "Same Peer", 10990100),
                10990100,
                1
            );

            state.RegisterClient(ServerType.Area, mover);
            state.RegisterClient(ServerType.Area, peer);

            await using var runDb = new MainContext(options);
            var handler = new AreaAvatarMoveRequestHandler(
                state,
                CreateDirectMapLinkTransitionService(runDb, state),
                CreateScriptedEventTriggerService(runDb)
            );
            var moves = new[]
            {
                new MovementData(1f, 2f, 3f, 4, MovementType.Running),
                new MovementData(5f, 6f, 7f, 8, MovementType.Walking),
            };

            await handler.HandleAsync(
                BuildAvatarMovePayload(moves),
                mover,
                TestContext.Current.CancellationToken
            );

            var notify = peer.Sent.Single(packet => packet.Type == PacketType.AvatarNotifyMove);
            var reader = new PacketReader(notify.Payload);
            Assert.Equal(2u, reader.ReadUInt());
            Assert.Equal(4001u, reader.ReadUInt());
            var firstMove = MovementData.FromBytes(reader.ReadBytes(14));
            Assert.Equal(MovementType.Running, firstMove.Animation);
            Assert.Equal(4001u, reader.ReadUInt());
            var secondMove = MovementData.FromBytes(reader.ReadBytes(14));
            Assert.Equal(MovementType.Walking, secondMove.Animation);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AvatarMoveHandler_BroadcastsOnlyWithinSameMapAndChannel()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var state = new SharedState();
            var mover = CreateSession(
                CreateUserWithCharacter(1, 4001, "move-user", "Mover", 10990100),
                10990100,
                1
            );
            var sameAreaPeer = CreateSession(
                CreateUserWithCharacter(2, 4002, "same-peer", "Same Peer", 10990100),
                10990100,
                1
            );
            var differentMapPeer = CreateSession(
                CreateUserWithCharacter(3, 4003, "other-map", "Other Map", 10990110),
                10990110,
                1
            );
            var differentChannelPeer = CreateSession(
                CreateUserWithCharacter(4, 4004, "other-channel", "Other Channel", 10990100),
                10990100,
                2
            );

            state.RegisterClient(ServerType.Area, mover);
            state.RegisterClient(ServerType.Area, sameAreaPeer);
            state.RegisterClient(ServerType.Area, differentMapPeer);
            state.RegisterClient(ServerType.Area, differentChannelPeer);

            await using var runDb = new MainContext(options);
            var handler = new AreaAvatarMoveRequestHandler(
                state,
                CreateDirectMapLinkTransitionService(runDb, state),
                CreateScriptedEventTriggerService(runDb)
            );
            var move = new MovementData(9f, 8f, 7f, 6, MovementType.Running);

            await handler.HandleAsync(move.ToBytes(), mover, TestContext.Current.CancellationToken);

            Assert.Collection(
                sameAreaPeer.Sent,
                packet => Assert.Equal(PacketType.AvatarNotifyMove, packet.Type)
            );
            Assert.Empty(differentMapPeer.Sent);
            Assert.Empty(differentChannelPeer.Sent);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AvatarMoveHandler_CrossingDirectMapLink_PushesNotifyChangeMap_WhenClientDoesNotSendMapEnter()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 4101, "move-link-user", "Mover", 10990100);

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Source",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 180,
                    },
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
                    }
                );
                db.MapLinks.Add(
                    new MapLink
                    {
                        SourceMapId = 10990100,
                        ChannelId = 1,
                        PositionX = -9800f,
                        PositionY = 2f,
                        PositionZ = -18000f,
                        Yaw = 0,
                        Length = 300f,
                        Depth = 0f,
                        DestinationMapIds = "10990110",
                        SortOrder = 10,
                        IsEnabled = true,
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
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var mover = CreateSession(
                user,
                10990100,
                1,
                x: -10450f,
                y: 2f,
                z: -18000f,
                rotation: 0
            );
            var oldPeer = CreateSession(
                CreateUserWithCharacter(2, 4102, "old-peer", "Old Peer", 10990100),
                10990100,
                1
            );
            var differentChannelPeer = CreateSession(
                CreateUserWithCharacter(3, 4103, "other-channel", "Other Channel", 10990100),
                10990100,
                2
            );

            state.RegisterClient(ServerType.Area, mover);
            state.RegisterClient(ServerType.Area, oldPeer);
            state.RegisterClient(ServerType.Area, differentChannelPeer);

            await using var runDb = new MainContext(options);
            var handler = new AreaAvatarMoveRequestHandler(
                state,
                CreateDirectMapLinkTransitionService(runDb, state),
                CreateScriptedEventTriggerService(runDb)
            );

            await handler.HandleAsync(
                new MovementData(-9800f, 2f, -18000f, 0, MovementType.Running).ToBytes(),
                mover,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                mover.Sent,
                packet => Assert.Equal(PacketType.NotifyChangeMap, packet.Type)
            );
            var notify = OutgoingPacketTestParsers.ParseNotifyChangeMap(mover.Sent[0].Payload);
            Assert.Equal((ushort)0, notify.AreaServerInfo.Port);
            Assert.Equal(string.Empty, notify.AreaServerInfo.IP);
            Assert.Collection(
                oldPeer.Sent,
                packet => Assert.Equal(PacketType.NotifyDisappearChara, packet.Type)
            );
            Assert.Empty(differentChannelPeer.Sent);
            Assert.Equal(10990110u, mover.MapId);
            Assert.Equal(-11000f, mover.X);
            Assert.Equal(0.1f, mover.Y);
            Assert.Equal(-19200f, mover.Z);
            Assert.Equal(10990110u, mover.Character!.CurrentMapId);
            Assert.False(mover.HasMovedSinceMapLoad);
            Assert.True(mover.IsMapTransitionPending);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AvatarMoveHandler_CrossingSelectorMapLink_OpensAreaMapSelector()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 4201, "move-selector-user", "Mover", 10990100);

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Source",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 180,
                    },
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination One",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
                    },
                    new Map
                    {
                        MapId = 10990200,
                        Name = "Destination Two",
                        SpawnX = -9600f,
                        SpawnY = 0.1f,
                        SpawnZ = -8400f,
                        SpawnRotation = 90,
                    }
                );
                db.MapLinks.Add(
                    new MapLink
                    {
                        SourceMapId = 10990100,
                        ChannelId = 1,
                        PositionX = -9800f,
                        PositionY = 2f,
                        PositionZ = -18000f,
                        Yaw = 0,
                        Length = 300f,
                        Depth = 0f,
                        DestinationMapIds = "10990110,10990200",
                        Behavior = MapLinkBehavior.ForceSelection,
                        SortOrder = 10,
                        IsEnabled = true,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var mover = CreateSession(
                user,
                10990100,
                1,
                x: -10450f,
                y: 2f,
                z: -18000f,
                rotation: 0
            );
            var peer = CreateSession(
                CreateUserWithCharacter(2, 4202, "selector-peer", "Selector Peer", 10990100),
                10990100,
                1
            );

            state.RegisterClient(ServerType.Area, mover);
            state.RegisterClient(ServerType.Area, peer);

            await using var runDb = new MainContext(options);
            var handler = new AreaAvatarMoveRequestHandler(
                state,
                CreateDirectMapLinkTransitionService(runDb, state),
                CreateScriptedEventTriggerService(runDb)
            );

            await handler.HandleAsync(
                new MovementData(-9800f, 2f, -18000f, 0, MovementType.Running).ToBytes(),
                mover,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                mover.Sent,
                packet => Assert.Equal(PacketType.SelectInitIslandStart, packet.Type),
                packet => Assert.Equal(PacketType.EventAreaMapSelectExec, packet.Type)
            );

            var islandStart = OutgoingPacketTestParsers.ParseSelectInitIslandStartNotify(
                mover.Sent[0].Payload
            );
            Assert.Collection(
                islandStart.Islands,
                island =>
                {
                    Assert.Equal(1u, island.IslandId);
                    Assert.Equal(
                        "Akihabara Station Front Street Event Area Island 1",
                        island.Title
                    );
                    Assert.Equal(
                        "Akihabara Station Front Street Event Area\nAkihabara UDX",
                        island.Description
                    );
                }
            );

            var selector = OutgoingPacketTestParsers.ParseEventAreaMapSelectExecNotify(
                mover.Sent[1].Payload
            );
            Assert.Equal([10990110u, 10990200u], selector.MapIds);
            Assert.Equal(1u, selector.IslandId);
            Assert.Equal(0u, selector.IsRegisteredIsland);

            Assert.NotNull(mover.PendingAreaMapSelection);
            Assert.Equal(2, mover.PendingAreaMapSelection!.Destinations.Count);
            Assert.Equal(1u, mover.PendingAreaMapSelection.IslandId);
            Assert.True(mover.PendingAreaMapSelection.AwaitingIslandBootstrapAck);
            Assert.True(mover.PendingAreaMapSelection.SelectorOpened);
            Assert.True(mover.HasMovedSinceMapLoad);
            Assert.False(mover.IsMapTransitionPending);
            Assert.Empty(peer.Sent);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AvatarMoveHandler_EnteringSelectorLinkWithOneValidDestination_FallsBackToDirectTravel()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(
                1,
                7251,
                "selector-collapse-user",
                "Selector Collapse",
                10990100
            );

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Source",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 180,
                    },
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination One",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
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
                db.MapLinks.Add(
                    new MapLink
                    {
                        SourceMapId = 10990100,
                        ChannelId = 1,
                        PositionX = -9800f,
                        PositionY = 2f,
                        PositionZ = -18000f,
                        Yaw = 0,
                        Length = 300f,
                        Depth = 0f,
                        DestinationMapIds = "10990110,10990200,10990210",
                        Behavior = MapLinkBehavior.ForceSelection,
                        SortOrder = 10,
                        IsEnabled = true,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var session = CreateSession(
                user,
                10990100,
                1,
                x: -10450f,
                y: 2f,
                z: -18000f,
                rotation: 0
            );

            state.RegisterClient(ServerType.Area, session);

            await using var runDb = new MainContext(options);
            var handler = new AreaAvatarMoveRequestHandler(
                state,
                CreateDirectMapLinkTransitionService(runDb, state),
                CreateScriptedEventTriggerService(runDb)
            );

            await handler.HandleAsync(
                new MovementData(-9800f, 2f, -18000f, 0, MovementType.Running).ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                session.Sent,
                packet => Assert.Equal(PacketType.NotifyChangeMap, packet.Type)
            );

            var notify = OutgoingPacketTestParsers.ParseNotifyChangeMap(session.Sent[0].Payload);
            Assert.Equal(10990110u, notify.MapId);
            Assert.Equal(1u, notify.ChannelId);
            Assert.Equal(10990110u, session.MapId);
            Assert.Null(session.PendingAreaMapSelection);
            Assert.True(session.IsMapTransitionPending);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AvatarMoveHandler_NearDirectMapLinkButOutsideRectangle_DoesNotTransition()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var user = CreateUserWithCharacter(1, 4251, "move-near-user", "Mover", 10990100);

            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Maps.AddRange(
                    new Map
                    {
                        MapId = 10990100,
                        Name = "Source",
                        SpawnX = -9100f,
                        SpawnY = 2f,
                        SpawnZ = -18000f,
                        SpawnRotation = 180,
                    },
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination",
                        SpawnX = -11000f,
                        SpawnY = 0.1f,
                        SpawnZ = -19200f,
                        SpawnRotation = 0,
                    }
                );
                db.MapLinks.Add(
                    new MapLink
                    {
                        SourceMapId = 10990100,
                        ChannelId = 1,
                        PositionX = -8677f,
                        PositionY = 2f,
                        PositionZ = -19312f,
                        Yaw = 0,
                        Length = 300f,
                        Depth = 100f,
                        DestinationMapIds = "10990110",
                        SortOrder = 10,
                        IsEnabled = true,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                await MarkIntroductionEventCompletedAsync(
                    options,
                    4251,
                    TestContext.Current.CancellationToken
                );
            }

            var state = new SharedState();
            var mover = CreateSession(user, 10990100, 1, x: -9100f, y: 2f, z: -18780f, rotation: 0);
            var sameAreaPeer = CreateSession(
                CreateUserWithCharacter(2, 4252, "same-peer", "Same Peer", 10990100),
                10990100,
                1
            );

            state.RegisterClient(ServerType.Area, mover);
            state.RegisterClient(ServerType.Area, sameAreaPeer);

            await using var runDb = new MainContext(options);
            var handler = new AreaAvatarMoveRequestHandler(
                state,
                CreateDirectMapLinkTransitionService(runDb, state),
                CreateScriptedEventTriggerService(runDb)
            );

            await handler.HandleAsync(
                new MovementData(-8918f, 2f, -18718f, 0, MovementType.Running).ToBytes(),
                mover,
                TestContext.Current.CancellationToken
            );

            Assert.Empty(mover.Sent);
            Assert.Collection(
                sameAreaPeer.Sent,
                packet => Assert.Equal(PacketType.AvatarNotifyMove, packet.Type)
            );
            Assert.Equal(10990100u, mover.MapId);
            Assert.Equal(-8918f, mover.X);
            Assert.Equal(2f, mover.Y);
            Assert.Equal(-18718f, mover.Z);
            Assert.Equal(10990100u, mover.Character!.CurrentMapId);
            Assert.True(mover.HasMovedSinceMapLoad);
            Assert.False(mover.IsMapTransitionPending);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task EmotionHandler_BroadcastsOnlyWithinSameMapAndChannel_IncludingSender()
    {
        var state = new SharedState();
        var sender = CreateSession(
            CreateUserWithCharacter(1, 5001, "emotion-user", "Emotion User", 10990100),
            10990100,
            1
        );
        var sameAreaPeer = CreateSession(
            CreateUserWithCharacter(2, 5002, "same-peer", "Same Peer", 10990100),
            10990100,
            1
        );
        var differentMapPeer = CreateSession(
            CreateUserWithCharacter(3, 5003, "other-map", "Other Map", 10990110),
            10990110,
            1
        );
        var differentChannelPeer = CreateSession(
            CreateUserWithCharacter(4, 5004, "other-channel", "Other Channel", 10990100),
            10990100,
            2
        );

        state.RegisterClient(ServerType.Area, sender);
        state.RegisterClient(ServerType.Area, sameAreaPeer);
        state.RegisterClient(ServerType.Area, differentMapPeer);
        state.RegisterClient(ServerType.Area, differentChannelPeer);

        var roboRepository = new Mock<IRoboRepository>();
        var handler = new AreaEmotionCharaHandler(state, roboRepository.Object);

        await handler.HandleAsync(
            BuildUIntPairPayload(sender.CharacterId, 77),
            sender,
            TestContext.Current.CancellationToken
        );

        Assert.Collection(
            sender.Sent,
            packet =>
            {
                Assert.Equal(PacketType.EmotionCharaResponse, packet.Type);
                var reader = new PacketReader(packet.Payload);
                Assert.Equal(sender.CharacterId, reader.ReadUInt());
                Assert.Equal(0u, reader.ReadUInt());
            },
            packet =>
            {
                Assert.Equal(PacketType.NotifyEmotionChara, packet.Type);
                var reader = new PacketReader(packet.Payload);
                Assert.Equal(sender.CharacterId, reader.ReadUInt());
                Assert.Equal(77u, reader.ReadUInt());
            }
        );
        Assert.Collection(
            sameAreaPeer.Sent,
            packet =>
            {
                Assert.Equal(PacketType.NotifyEmotionChara, packet.Type);
                var reader = new PacketReader(packet.Payload);
                Assert.Equal(sender.CharacterId, reader.ReadUInt());
                Assert.Equal(77u, reader.ReadUInt());
            }
        );
        Assert.Empty(differentMapPeer.Sent);
        Assert.Empty(differentChannelPeer.Sent);
    }

    [Fact]
    public async Task EmotionHandler_PreservesOwnedRoboObjectId()
    {
        var state = new SharedState();
        var sender = CreateSession(
            CreateUserWithCharacter(1, 42, "emotion-user", "Emotion User", 20000000),
            20000000,
            1
        );
        var sameAreaPeer = CreateSession(
            CreateUserWithCharacter(2, 5002, "same-peer", "Same Peer", 20000000),
            20000000,
            1
        );
        sender.MyRoomId = 42;
        sameAreaPeer.MyRoomId = 42;
        var roboObjectId = RoboRepository.GetObjectId(sender.CharacterId, 1);
        var roboRepository = new Mock<IRoboRepository>();
        roboRepository
            .Setup(x =>
                x.ExistsAsync(checked((int)sender.CharacterId), 1, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(true);

        state.RegisterClient(ServerType.Area, sender);
        state.RegisterClient(ServerType.Area, sameAreaPeer);

        var handler = new AreaEmotionCharaHandler(state, roboRepository.Object);
        await handler.HandleAsync(
            BuildUIntPairPayload(roboObjectId, 27),
            sender,
            TestContext.Current.CancellationToken
        );

        Assert.Collection(
            sender.Sent,
            packet =>
            {
                Assert.Equal(PacketType.EmotionCharaResponse, packet.Type);
                var reader = new PacketReader(packet.Payload);
                Assert.Equal(roboObjectId, reader.ReadUInt());
                Assert.Equal(0u, reader.ReadUInt());
            },
            packet =>
            {
                Assert.Equal(PacketType.NotifyEmotionChara, packet.Type);
                var reader = new PacketReader(packet.Payload);
                Assert.Equal(roboObjectId, reader.ReadUInt());
                Assert.Equal(27u, reader.ReadUInt());
            }
        );
        Assert.Collection(
            sameAreaPeer.Sent,
            packet =>
            {
                Assert.Equal(PacketType.NotifyEmotionChara, packet.Type);
                var reader = new PacketReader(packet.Payload);
                Assert.Equal(roboObjectId, reader.ReadUInt());
                Assert.Equal(27u, reader.ReadUInt());
            }
        );
    }

    [Fact]
    public async Task EmotionHandler_RejectsUnownedTargetObjectId()
    {
        var state = new SharedState();
        var sender = CreateSession(
            CreateUserWithCharacter(1, 42, "emotion-user", "Emotion User", 20000000),
            20000000,
            1
        );
        var sameAreaPeer = CreateSession(
            CreateUserWithCharacter(2, 5002, "same-peer", "Same Peer", 20000000),
            20000000,
            1
        );
        var otherRoboObjectId = RoboRepository.GetObjectId(sameAreaPeer.CharacterId, 1);
        var roboRepository = new Mock<IRoboRepository>();

        state.RegisterClient(ServerType.Area, sender);
        state.RegisterClient(ServerType.Area, sameAreaPeer);

        var handler = new AreaEmotionCharaHandler(state, roboRepository.Object);
        await handler.HandleAsync(
            BuildUIntPairPayload(otherRoboObjectId, 27),
            sender,
            TestContext.Current.CancellationToken
        );

        Assert.Collection(
            sender.Sent,
            packet =>
            {
                Assert.Equal(PacketType.EmotionCharaResponse, packet.Type);
                var reader = new PacketReader(packet.Payload);
                Assert.Equal(otherRoboObjectId, reader.ReadUInt());
                Assert.Equal(1u, reader.ReadUInt());
            }
        );
        Assert.Empty(sameAreaPeer.Sent);
    }

    [Fact]
    public async Task AvatarProfileGetDataHandler_OnlyResolvesTargetsInSameMapAndChannel()
    {
        var state = new SharedState();
        var requester = CreateSession(
            CreateUserWithCharacter(1, 6001, "profile-user", "Profile User", 10990100),
            10990100,
            1
        );
        var visibleTarget = CreateSession(
            CreateUserWithCharacter(
                2,
                6002,
                "visible-target",
                "Visible",
                10990100,
                like1: "Apples"
            ),
            10990100,
            1
        );
        var hiddenTarget = CreateSession(
            CreateUserWithCharacter(3, 6003, "hidden-target", "Hidden", 10990100, like1: "Secret"),
            10990100,
            2
        );

        state.RegisterClient(ServerType.Area, requester);
        state.RegisterClient(ServerType.Area, visibleTarget);
        state.RegisterClient(ServerType.Area, hiddenTarget);

        var handler = new AreaAvatarProfileGetDataHandler(state);

        await handler.HandleAsync(
            BuildUIntPayload(visibleTarget.CharacterId),
            requester,
            TestContext.Current.CancellationToken
        );
        var visibleReader = new PacketReader(requester.Sent[^1].Payload);
        Assert.Equal(0u, visibleReader.ReadUInt());
        Assert.Equal(visibleTarget.CharacterId, visibleReader.ReadUInt());
        Assert.Equal("Apples", visibleReader.ReadFixedString(31, "shift_jis"));

        requester.Sent.Clear();

        await handler.HandleAsync(
            BuildUIntPayload(hiddenTarget.CharacterId),
            requester,
            TestContext.Current.CancellationToken
        );
        var hiddenReader = new PacketReader(requester.Sent[^1].Payload);
        Assert.Equal(0u, hiddenReader.ReadUInt());
        Assert.Equal(hiddenTarget.CharacterId, hiddenReader.ReadUInt());
        Assert.Equal(string.Empty, hiddenReader.ReadFixedString(31, "shift_jis"));
    }

    private static CapturingPlayerSession CreateSession(
        User user,
        uint mapId,
        int channelId,
        float x = 0f,
        float y = 0f,
        float z = 0f,
        int rotation = 0
    )
    {
        var character = user.Characters.Single();

        return new CapturingPlayerSession
        {
            User = user,
            UserId = user.Id,
            Character = character,
            CharacterId = (uint)character.Id,
            MapId = mapId,
            ChannelId = channelId,
            X = x,
            Y = y,
            Z = z,
            Rotation = rotation,
        };
    }

    [Fact]
    public async Task AreasvEnterHandler_InvalidOtp_SendsEightByteEnterResponse()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            var session = new CapturingPlayerSession();
            await using var handlerDb = new MainContext(options);
            var handler = new AreasvEnterHandler(
                new UserSessionRepository(handlerDb, NullLogger<UserSessionRepository>.Instance),
                new MapRepository(handlerDb),
                new ChannelRepository(handlerDb),
                new CharacterRepository(handlerDb, NullLogger<CharacterRepository>.Instance),
                new MyRoomRepository(handlerDb),
                new CircleRepository(handlerDb),
                new FriendRepository(handlerDb),
                new SharedState(),
                NullLogger<AreasvEnterHandler>.Instance
            );

            await handler.HandleAsync(
                BuildAreasvEnterPayload(1, "unknown-otp-12345678"),
                session,
                TestContext.Current.CancellationToken
            );

            // recv_enter_areasv_r is a fixed 8-byte read on the client (result + objId).
            var reply = Assert.Single(session.Sent, p => p.Type == PacketType.AreasvEnterResponse);
            Assert.Equal(8, reply.Payload.Length);
            Assert.NotEqual(0u, new PacketReader(reply.Payload).ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UploadRateHandlers_SendConfiguredPercent()
    {
        var options = Options.Create(
            new ServerOptions { AiUploadRatePercent = 70, AdventureUploadRatePercent = 250 }
        );
        var session = new CapturingPlayerSession();

        await new AreaAiUploadRateGetHandler(options).HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );
        await new AreaAdventureUploadRateGetHandler(options).HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        // The client reads a fixed 4-byte percentage and computes price * rate / 100.
        var ai = Assert.Single(session.Sent, p => p.Type == PacketType.AiUploadRateGetResponse);
        Assert.Equal(4, ai.Payload.Length);
        Assert.Equal(70u, new PacketReader(ai.Payload).ReadUInt());

        var adventure = Assert.Single(
            session.Sent,
            p => p.Type == PacketType.AdventureUploadRateGetResponse
        );
        Assert.Equal(4, adventure.Payload.Length);
        Assert.Equal(100u, new PacketReader(adventure.Payload).ReadUInt());
    }

    private static User CreateUserWithCharacter(
        int userId,
        int characterId,
        string username,
        string characterName,
        uint currentMapId,
        string like1 = "Like 1"
    )
    {
        var user = new User { Id = userId, Username = username };
        user.SetPassword("pw");

        var character = new Character
        {
            Id = characterId,
            Name = characterName,
            User = user,
            UserId = userId,
            CurrentMapId = currentMapId,
            ModelId = 100,
            Birthdate = new DateTime(2000, 1, 2),
            BloodType = BloodType.A,
            Gender = 1,
            FaceType = 1,
            Hairstyle = 2,
            Like1 = like1,
            Like2 = "Like 2",
            Like3 = "Like 3",
            LikeDesc1 = "Desc 1",
            LikeDesc2 = "Desc 2",
            LikeDesc3 = "Desc 3",
            AvatarDesc = "Hello there",
        };

        user.Characters.Add(character);
        return user;
    }

    private static byte[] BuildAvatarMovePayload(IReadOnlyList<MovementData> moves)
    {
        var writer = new PacketWriter();
        foreach (var move in moves)
            writer.Write(move.ToBytes());
        return writer.ToBytes();
    }

    private static byte[] BuildUIntPairPayload(uint first, uint second)
    {
        var writer = new PacketWriter();
        writer.Write(first);
        writer.Write(second);
        return writer.ToBytes();
    }

    private static byte[] BuildAreasvEnterPayload(uint userId, string otp)
    {
        var writer = new PacketWriter();
        writer.Write(userId);
        writer.WriteFixedString(otp, 20, "ASCII");
        return writer.ToBytes();
    }

    private static byte[] BuildUIntPayload(uint value)
    {
        var writer = new PacketWriter();
        writer.Write(value);
        return writer.ToBytes();
    }

    private static async Task MarkIntroductionEventCompletedAsync(
        DbContextOptions<MainContext> options,
        int characterId,
        CancellationToken ct
    )
    {
        await using var db = new MainContext(options);
        db.CharacterEventStatuses.Add(
            new CharacterEventStatus
            {
                CharacterId = characterId,
                EventKey = ScriptedEvents.Keys.IntroductionRin01,
                CompletedAtUtc = DateTime.UtcNow,
            }
        );
        await db.SaveChangesAsync(ct);
    }

    private static ScriptedEventTriggerService CreateScriptedEventTriggerService(MainContext db) =>
        new(new CharacterEventRepository(db), NullLogger<ScriptedEventTriggerService>.Instance);

    private static ServerScriptDispatcher CreateServerScriptDispatcher(MainContext db)
    {
        var eventRepository = new CharacterEventRepository(db);
        var serverScriptSession = new ServerScriptSession(
            eventRepository,
            NullLogger<ServerScriptSession>.Instance
        );
        var characterRepository = new CharacterRepository(
            db,
            NullLogger<CharacterRepository>.Instance
        );
        var mapRepository = new MapRepository(db);
        var shinjuRegistrationScript = new ShinjuRegistrationServerScript(
            characterRepository,
            eventRepository,
            mapRepository,
            serverScriptSession,
            TestTextLocaliser.English,
            NullLogger<ShinjuRegistrationServerScript>.Instance
        );
        return new ServerScriptDispatcher(
            [shinjuRegistrationScript],
            serverScriptSession,
            NullLogger<ServerScriptDispatcher>.Instance
        );
    }

    private static DirectMapLinkTransitionService CreateDirectMapLinkTransitionService(
        MainContext db,
        SharedState state
    )
    {
        return new DirectMapLinkTransitionService(
            new MapRepository(db),
            new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
            new MyRoomRepository(db),
            new CircleRepository(db),
            new MapLinkRepository(db),
            new ChannelRepository(db),
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

    private static IReadOnlyList<(
        uint MapId,
        ushort AreaServerPort,
        string AreaServerIp,
        uint ChannelId,
        uint RouteMapId,
        uint MapSerialId,
        uint RouteState,
        float PositionX,
        float PositionY,
        float PositionZ,
        int Yaw,
        byte Animation
    )> ReadSelectMapEntries(byte[] payload)
    {
        var reader = new PacketReader(payload);
        var count = reader.ReadUInt();
        var entries = new List<(
            uint MapId,
            ushort AreaServerPort,
            string AreaServerIp,
            uint ChannelId,
            uint RouteMapId,
            uint MapSerialId,
            uint RouteState,
            float PositionX,
            float PositionY,
            float PositionZ,
            int Yaw,
            byte Animation
        )>((int)count);
        for (var index = 0; index < count; index++)
        {
            var entry = NotifySelectMapEntry.FromBytes(
                reader.ReadBytes(NotifySelectMapEntry.PacketSize)
            );
            entries.Add(
                (
                    entry.MapId,
                    entry.AreaServerInfo.Port,
                    entry.AreaServerInfo.IP,
                    entry.ChannelId,
                    entry.RouteMapId,
                    entry.MapSerialId,
                    entry.RouteState,
                    entry.PositionX,
                    entry.PositionY,
                    entry.PositionZ,
                    entry.Yaw,
                    entry.Animation
                )
            );
        }

        return entries;
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose() { }
        }
    }
}
