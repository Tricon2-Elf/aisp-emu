using AISpace.Common.Config;
using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using AISpace.Network.Packets.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AISpace.Common.Tests;

public class AreaMapHandlersTests
{
    [Fact]
    public async Task MapLinkGetDataHandler_SendsDirectLinksInOrder_AndSkipsUnsupportedSelectorRows()
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
                        Yaw = 5,
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
                        Yaw = 15,
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
                        Yaw = 25,
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
                        SpawnRotation = 90,
                    },
                    new Map
                    {
                        MapId = 10990210,
                        Name = "Destination Two",
                        SpawnX = -9600f,
                        SpawnY = 0.1f,
                        SpawnZ = -8400f,
                        SpawnRotation = 45,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var session = CreateSession(CreateUserWithCharacter(1, 1001, "link-user", "Link Tester", 10990100), 10990100, 0);
            var logger = new ListLogger<AreaMapLinkGetDataHandler>();
            var handler = new AreaMapLinkGetDataHandler(
                new MapLinkRepository(new MainContext(options)),
                new MapRepository(new MainContext(options)),
                new ChannelRepository(new MainContext(options)),
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

            await handler.HandleAsync(BuildUIntPairPayload(10990100, 1), session, TestContext.Current.CancellationToken);

            Assert.Collection(session.Sent, packet => Assert.Equal(PacketType.MapLinkGetDataResponse, packet.Type), packet => Assert.Equal(PacketType.MapLinkNotifyData, packet.Type), packet => Assert.Equal(PacketType.MapLinkNotifyData, packet.Type), packet => Assert.Equal(PacketType.NotifySelectMap, packet.Type));

            var firstLink = MapLinkNotifyData.FromBytes(session.Sent[1].Payload);
            Assert.Equal(10f, firstLink.Data.PositionX);
            Assert.Equal(20f, firstLink.Data.PositionZ);
            Assert.Equal((byte)5, firstLink.Data.Yaw);

            var secondLink = MapLinkNotifyData.FromBytes(session.Sent[2].Payload);
            Assert.Equal(50f, secondLink.Data.PositionX);
            Assert.Equal(60f, secondLink.Data.PositionZ);
            Assert.Equal((byte)25, secondLink.Data.Yaw);

            Assert.Collection(
                ReadSelectMapEntries(session.Sent[3].Payload),
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
                    Assert.Equal((byte)90, entry.Yaw);
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
                    Assert.Equal((byte)45, entry.Yaw);
                    Assert.Equal((byte)0, entry.Animation);
                }
            );
            Assert.Equal(10990100u, session.MapId);
            Assert.Equal(1, session.ChannelId);
            Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("Skipping MapLink"));
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
                        SpawnRotation = 4,
                    },
                    new Map
                    {
                        MapId = 10990110,
                        Name = "Destination",
                        SpawnX = 11f,
                        SpawnY = 12f,
                        SpawnZ = 13f,
                        SpawnRotation = 14,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var session = CreateSession(user, 10990100, 1, x: 101f, y: 5f, z: 202f, rotation: 8);
            var oldPeer = CreateSession(CreateUserWithCharacter(2, 2002, "old-peer", "Old Peer", 10990100), 10990100, 1);
            var differentChannelPeer = CreateSession(CreateUserWithCharacter(3, 2003, "other-channel", "Other Channel", 10990100), 10990100, 2);
            var destinationPeer = CreateSession(CreateUserWithCharacter(4, 2004, "dest-peer", "Dest Peer", 10990110), 10990110, 1);

            state.RegisterClient("Area", session);
            state.RegisterClient("Area", oldPeer);
            state.RegisterClient("Area", differentChannelPeer);
            state.RegisterClient("Area", destinationPeer);

            var handler = new AreaMapEnterHandler(new MapRepository(new MainContext(options)), CreateDirectMapLinkTransitionService(options, state), NullLogger<AreaMapEnterHandler>.Instance);

            await handler.HandleAsync(BuildUIntPairPayload(10990110, 1), session, TestContext.Current.CancellationToken);

            Assert.Single(session.Sent);
            Assert.Equal(PacketType.MapEnterResponse, session.Sent[0].Type);
            Assert.Equal(0u, new PacketReader(session.Sent[0].Payload).ReadUInt());
            Assert.Equal(10990110u, session.MapId);
            Assert.Equal(1, session.ChannelId);
            Assert.Equal(11f, session.X);
            Assert.Equal(12f, session.Y);
            Assert.Equal(13f, session.Z);
            Assert.Equal((sbyte)14, session.Rotation);
            Assert.Equal(10990110u, session.Character!.CurrentMapId);

            Assert.Collection(oldPeer.Sent, packet => Assert.Equal(PacketType.NotifyDisappearChara, packet.Type));
            Assert.Empty(differentChannelPeer.Sent);
            Assert.Empty(destinationPeer.Sent);

            await using var verifyDb = new MainContext(options);
            var persistedCharacter = await verifyDb.Characters.SingleAsync(c => c.Id == session.Character.Id, TestContext.Current.CancellationToken);
            Assert.Equal(10990110u, persistedCharacter.CurrentMapId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task MapEnterHandler_CurrentMapRequest_ResolvesDirectMapLink_AndPushesNotifyChangeMap()
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
                        SpawnRotation = 90,
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
            var session = CreateSession(user, 10990100, 1, x: -9100f, y: 2f, z: -18000f, rotation: 0);
            session.HasMovedSinceMapLoad = true;
            var oldPeer = CreateSession(CreateUserWithCharacter(2, 7002, "old-peer", "Old Peer", 10990100), 10990100, 1);

            state.RegisterClient("Area", session);
            state.RegisterClient("Area", oldPeer);

            var handler = new AreaMapEnterHandler(new MapRepository(new MainContext(options)), CreateDirectMapLinkTransitionService(options, state), NullLogger<AreaMapEnterHandler>.Instance);

            await handler.HandleAsync(BuildUIntPairPayload(10990100, 1), session, TestContext.Current.CancellationToken);

            Assert.Collection(session.Sent, packet => Assert.Equal(PacketType.MapEnterResponse, packet.Type), packet => Assert.Equal(PacketType.NotifyChangeMap, packet.Type));

            var responseReader = new PacketReader(session.Sent[0].Payload);
            Assert.Equal(0u, responseReader.ReadUInt());

            var notify = NotifyChangeMap.FromBytes(session.Sent[1].Payload);
            Assert.Equal(1u, notify.ChannelId);
            Assert.Equal(10990110u, notify.MapId);
            Assert.Equal(10990110u, notify.MapSerialId);
            Assert.Equal(-11000f, notify.PositionX);
            Assert.Equal(0.1f, notify.PositionY);
            Assert.Equal(-19200f, notify.PositionZ);
            Assert.Equal((sbyte)0, notify.Rotation);
            Assert.Equal((byte)MovementType.Stopped, notify.Animation);
            Assert.Equal((byte)2, notify.Flag);
            Assert.Equal((ushort)50054, notify.AreaServerInfo.Port);
            Assert.Equal("localhost", notify.AreaServerInfo.IP);
            Assert.Equal((byte)2, notify.FadeFlag);

            Assert.Equal(10990110u, session.MapId);
            Assert.Equal(1, session.ChannelId);
            Assert.Equal(-11000f, session.X);
            Assert.Equal(0.1f, session.Y);
            Assert.Equal(-19200f, session.Z);
            Assert.Equal(10990110u, session.Character!.CurrentMapId);

            Assert.Collection(oldPeer.Sent, packet => Assert.Equal(PacketType.NotifyDisappearChara, packet.Type));
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
                        SpawnRotation = 90,
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
            var session = CreateSession(user, 10990100, 1, x: -9055f, y: 2f, z: -17988f, rotation: 0);
            session.HasMovedSinceMapLoad = false;

            state.RegisterClient("Area", session);

            var handler = new AreaMapEnterHandler(new MapRepository(new MainContext(options)), CreateDirectMapLinkTransitionService(options, state), NullLogger<AreaMapEnterHandler>.Instance);

            await handler.HandleAsync(BuildUIntPairPayload(10990100, 1), session, TestContext.Current.CancellationToken);

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
    public async Task MapDataEnterEndHandler_SpawnsOnlyPeersInDestinationArea()
    {
        var state = new SharedState();
        var session = CreateSession(CreateUserWithCharacter(1, 3001, "spawn-user", "Spawn User", 10990110), 10990110, 1, x: 1f, y: 2f, z: 3f);
        session.NeedsPostLoadSelfAvatarNotify = false;
        var destinationPeer = CreateSession(CreateUserWithCharacter(2, 3002, "spawn-peer", "Spawn Peer", 10990110), 10990110, 1, x: 4f, y: 5f, z: 6f);
        var oldPeer = CreateSession(CreateUserWithCharacter(3, 3003, "old-peer", "Old Peer", 10990100), 10990100, 1);
        var differentChannelPeer = CreateSession(CreateUserWithCharacter(4, 3004, "other-channel", "Other Channel", 10990110), 10990110, 2);

        state.RegisterClient("Area", session);
        state.RegisterClient("Area", destinationPeer);
        state.RegisterClient("Area", oldPeer);
        state.RegisterClient("Area", differentChannelPeer);

        var handler = new AreaMapDataEnterEndHandler(state, NullLogger<AreaMapDataEnterEndHandler>.Instance);

        await handler.HandleAsync(ReadOnlyMemory<byte>.Empty, session, TestContext.Current.CancellationToken);

        Assert.Collection(
            session.Sent,
            packet => Assert.Equal(PacketType.MapDataEnterEndResponse, packet.Type),
            packet =>
            {
                Assert.Equal(PacketType.AvatarNotifyData, packet.Type);
                Assert.Equal(1u, new PacketReader(packet.Payload).ReadUInt());
            }
        );
        Assert.Collection(destinationPeer.Sent, packet => Assert.Equal(PacketType.AvatarNotifyData, packet.Type));
        Assert.Empty(oldPeer.Sent);
        Assert.Empty(differentChannelPeer.Sent);
    }

    [Fact]
    public async Task MapDataEnterEndHandler_SendsSelfAvatarNotify_EvenWithoutVisiblePeers()
    {
        var state = new SharedState();
        var session = CreateSession(CreateUserWithCharacter(1, 3051, "solo-user", "Solo User", 10990110), 10990110, 1, x: 11f, y: 12f, z: 13f);
        session.NeedsPostLoadSelfAvatarNotify = true;

        state.RegisterClient("Area", session);

        var handler = new AreaMapDataEnterEndHandler(state, NullLogger<AreaMapDataEnterEndHandler>.Instance);

        await handler.HandleAsync(ReadOnlyMemory<byte>.Empty, session, TestContext.Current.CancellationToken);

        Assert.Collection(
            session.Sent,
            packet => Assert.Equal(PacketType.MapDataEnterEndResponse, packet.Type),
            packet =>
            {
                Assert.Equal(PacketType.AvatarNotifyData, packet.Type);
                Assert.Equal(0u, new PacketReader(packet.Payload).ReadUInt());
            }
        );
    }

    [Fact]
    public async Task AreasvEnterHandler_UsesPendingTransitionSpawn_WithoutRandomSpread()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();

        try
        {
            const string otp = "transition-otp-1234";
            var user = CreateUserWithCharacter(1, 3061, "transition-user", "Transition User", 10990110);

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
            state.SetPendingAreaTransition(new SharedState.PendingAreaTransition(user.Id, 10990110, 1, -11000f, 0.1f, -19200f, 0));

            var session = new CapturingPlayerSession();
            var handler = new AreasvEnterHandler(new UserSessionRepository(new MainContext(options), new TestMainContextFactory(options), NullLogger<UserSessionRepository>.Instance), new MapRepository(new MainContext(options)), new CharacterRepository(new MainContext(options), NullLogger<CharacterRepository>.Instance), state, NullLogger<AreasvEnterHandler>.Instance);

            await handler.HandleAsync(BuildAreasvEnterPayload((uint)user.Id, otp), session, TestContext.Current.CancellationToken);

            Assert.Equal(10990110u, session.MapId);
            Assert.Equal(1, session.ChannelId);
            Assert.Equal(-11000f, session.X);
            Assert.Equal(0.1f, session.Y);
            Assert.Equal(-19200f, session.Z);
            Assert.Equal((sbyte)0, session.Rotation);
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.AreasvEnterResponse);
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
            var mover = CreateSession(CreateUserWithCharacter(1, 4001, "move-user", "Mover", 10990100), 10990100, 1);
            var sameAreaPeer = CreateSession(CreateUserWithCharacter(2, 4002, "same-peer", "Same Peer", 10990100), 10990100, 1);
            var differentMapPeer = CreateSession(CreateUserWithCharacter(3, 4003, "other-map", "Other Map", 10990110), 10990110, 1);
            var differentChannelPeer = CreateSession(CreateUserWithCharacter(4, 4004, "other-channel", "Other Channel", 10990100), 10990100, 2);

            state.RegisterClient("Area", mover);
            state.RegisterClient("Area", sameAreaPeer);
            state.RegisterClient("Area", differentMapPeer);
            state.RegisterClient("Area", differentChannelPeer);

            var handler = new AreaAvatarMoveRequestHandler(state, CreateDirectMapLinkTransitionService(options, state), NullLogger<AreaAvatarMoveRequestHandler>.Instance);
            var move = new MovementData(9f, 8f, 7f, 6, MovementType.Running);

            await handler.HandleAsync(move.ToBytes(), mover, TestContext.Current.CancellationToken);

            Assert.Collection(sameAreaPeer.Sent, packet => Assert.Equal(PacketType.AvatarNotifyMove, packet.Type));
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
                        SpawnRotation = 90,
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
            var mover = CreateSession(user, 10990100, 1, x: -10450f, y: 2f, z: -18000f, rotation: 0);
            var oldPeer = CreateSession(CreateUserWithCharacter(2, 4102, "old-peer", "Old Peer", 10990100), 10990100, 1);
            var differentChannelPeer = CreateSession(CreateUserWithCharacter(3, 4103, "other-channel", "Other Channel", 10990100), 10990100, 2);

            state.RegisterClient("Area", mover);
            state.RegisterClient("Area", oldPeer);
            state.RegisterClient("Area", differentChannelPeer);

            var handler = new AreaAvatarMoveRequestHandler(state, CreateDirectMapLinkTransitionService(options, state), NullLogger<AreaAvatarMoveRequestHandler>.Instance);

            await handler.HandleAsync(new MovementData(-9800f, 2f, -18000f, 0, MovementType.Running).ToBytes(), mover, TestContext.Current.CancellationToken);

            Assert.Collection(mover.Sent, packet => Assert.Equal(PacketType.MapEnterResponse, packet.Type), packet => Assert.Equal(PacketType.NotifyChangeMap, packet.Type));
            Assert.Collection(oldPeer.Sent, packet => Assert.Equal(PacketType.NotifyDisappearChara, packet.Type));
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
    public async Task EmotionHandler_BroadcastsOnlyWithinSameMapAndChannel_IncludingSender()
    {
        var state = new SharedState();
        var sender = CreateSession(CreateUserWithCharacter(1, 5001, "emotion-user", "Emotion User", 10990100), 10990100, 1);
        var sameAreaPeer = CreateSession(CreateUserWithCharacter(2, 5002, "same-peer", "Same Peer", 10990100), 10990100, 1);
        var differentMapPeer = CreateSession(CreateUserWithCharacter(3, 5003, "other-map", "Other Map", 10990110), 10990110, 1);
        var differentChannelPeer = CreateSession(CreateUserWithCharacter(4, 5004, "other-channel", "Other Channel", 10990100), 10990100, 2);

        state.RegisterClient("Area", sender);
        state.RegisterClient("Area", sameAreaPeer);
        state.RegisterClient("Area", differentMapPeer);
        state.RegisterClient("Area", differentChannelPeer);

        var handler = new AreaEmotionCharaHandler(state);

        await handler.HandleAsync(BuildUIntPairPayload(sender.CharacterId, 77), sender, TestContext.Current.CancellationToken);

        Assert.Collection(sender.Sent, packet => Assert.Equal(PacketType.EmotionCharaResponse, packet.Type), packet => Assert.Equal(PacketType.NotifyEmotionChara, packet.Type));
        Assert.Collection(sameAreaPeer.Sent, packet => Assert.Equal(PacketType.NotifyEmotionChara, packet.Type));
        Assert.Empty(differentMapPeer.Sent);
        Assert.Empty(differentChannelPeer.Sent);
    }

    [Fact]
    public async Task AvatarProfileGetDataHandler_OnlyResolvesTargetsInSameMapAndChannel()
    {
        var state = new SharedState();
        var requester = CreateSession(CreateUserWithCharacter(1, 6001, "profile-user", "Profile User", 10990100), 10990100, 1);
        var visibleTarget = CreateSession(CreateUserWithCharacter(2, 6002, "visible-target", "Visible", 10990100, like1: "Apples"), 10990100, 1);
        var hiddenTarget = CreateSession(CreateUserWithCharacter(3, 6003, "hidden-target", "Hidden", 10990100, like1: "Secret"), 10990100, 2);

        state.RegisterClient("Area", requester);
        state.RegisterClient("Area", visibleTarget);
        state.RegisterClient("Area", hiddenTarget);

        var handler = new AreaAvatarProfileGetDataHandler(state);

        await handler.HandleAsync(BuildUIntPayload(visibleTarget.CharacterId), requester, TestContext.Current.CancellationToken);
        var visibleReader = new PacketReader(requester.Sent[^1].Payload);
        Assert.Equal(0u, visibleReader.ReadUInt());
        Assert.Equal(visibleTarget.CharacterId, visibleReader.ReadUInt());
        Assert.Equal("Apples", visibleReader.ReadFixedString(31, "shift_jis"));

        requester.Sent.Clear();

        await handler.HandleAsync(BuildUIntPayload(hiddenTarget.CharacterId), requester, TestContext.Current.CancellationToken);
        var hiddenReader = new PacketReader(requester.Sent[^1].Payload);
        Assert.Equal(0u, hiddenReader.ReadUInt());
        Assert.Equal(hiddenTarget.CharacterId, hiddenReader.ReadUInt());
        Assert.Equal(string.Empty, hiddenReader.ReadFixedString(31, "shift_jis"));
    }

    private static CapturingPlayerSession CreateSession(User user, uint mapId, int channelId, float x = 0f, float y = 0f, float z = 0f, sbyte rotation = 0)
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

    private static User CreateUserWithCharacter(int userId, int characterId, string username, string characterName, uint currentMapId, string like1 = "Like 1")
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

    private static IReadOnlyList<(uint MapId, ushort AreaServerPort, string AreaServerIp, uint ChannelId, uint RouteMapId, uint MapSerialId, uint RouteState, float PositionX, float PositionY, float PositionZ, byte Yaw, byte Animation)> ReadSelectMapEntries(byte[] payload)
    {
        var reader = new PacketReader(payload);
        var count = reader.ReadUInt();
        var entries = new List<(uint MapId, ushort AreaServerPort, string AreaServerIp, uint ChannelId, uint RouteMapId, uint MapSerialId, uint RouteState, float PositionX, float PositionY, float PositionZ, byte Yaw, byte Animation)>((int)count);
        for (var index = 0; index < count; index++)
        {
            var entry = NotifySelectMapEntry.FromBytes(reader.ReadBytes(NotifySelectMapEntry.PacketSize));
            entries.Add((entry.MapId, entry.AreaServerInfo.Port, entry.AreaServerInfo.IP, entry.ChannelId, entry.RouteMapId, entry.MapSerialId, entry.RouteState, entry.PositionX, entry.PositionY, entry.PositionZ, entry.Yaw, entry.Animation));
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

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
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
