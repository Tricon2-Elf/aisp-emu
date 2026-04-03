using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var session = CreateSession(CreateUserWithCharacter(1, 1001, "link-user", "Link Tester", 10990100), 10990100, 0);
            var logger = new ListLogger<AreaMapLinkGetDataHandler>();
            var handler = new AreaMapLinkGetDataHandler(new MapLinkRepository(new MainContext(options)), logger);

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

            Assert.Equal(new uint[] { 10990110, 10990210 }, ReadSelectMapIds(session.Sent[3].Payload));
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

            var handler = new AreaMapEnterHandler(new MapRepository(new MainContext(options)), new CharacterRepository(new MainContext(options), NullLogger<CharacterRepository>.Instance), state, NullLogger<AreaMapEnterHandler>.Instance);

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
    public async Task MapDataEnterEndHandler_SpawnsOnlyPeersInDestinationArea()
    {
        var state = new SharedState();
        var session = CreateSession(CreateUserWithCharacter(1, 3001, "spawn-user", "Spawn User", 10990110), 10990110, 1, x: 1f, y: 2f, z: 3f);
        var destinationPeer = CreateSession(CreateUserWithCharacter(2, 3002, "spawn-peer", "Spawn Peer", 10990110), 10990110, 1, x: 4f, y: 5f, z: 6f);
        var oldPeer = CreateSession(CreateUserWithCharacter(3, 3003, "old-peer", "Old Peer", 10990100), 10990100, 1);
        var differentChannelPeer = CreateSession(CreateUserWithCharacter(4, 3004, "other-channel", "Other Channel", 10990110), 10990110, 2);

        state.RegisterClient("Area", session);
        state.RegisterClient("Area", destinationPeer);
        state.RegisterClient("Area", oldPeer);
        state.RegisterClient("Area", differentChannelPeer);

        var handler = new AreaMapDataEnterEndHandler(state, NullLogger<AreaMapDataEnterEndHandler>.Instance);

        await handler.HandleAsync(ReadOnlyMemory<byte>.Empty, session, TestContext.Current.CancellationToken);

        Assert.Collection(session.Sent, packet => Assert.Equal(PacketType.MapDataEnterEndResponse, packet.Type), packet => Assert.Equal(PacketType.AvatarNotifyData, packet.Type));
        Assert.Collection(destinationPeer.Sent, packet => Assert.Equal(PacketType.AvatarNotifyData, packet.Type));
        Assert.Empty(oldPeer.Sent);
        Assert.Empty(differentChannelPeer.Sent);
    }

    [Fact]
    public async Task AvatarMoveHandler_BroadcastsOnlyWithinSameMapAndChannel()
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

        var handler = new AreaAvatarMoveRequestHandler(state);
        var move = new MovementData(9f, 8f, 7f, 6, MovementType.Running);

        await handler.HandleAsync(move.ToBytes(), mover, TestContext.Current.CancellationToken);

        Assert.Collection(sameAreaPeer.Sent, packet => Assert.Equal(PacketType.AvatarNotifyMove, packet.Type));
        Assert.Empty(differentMapPeer.Sent);
        Assert.Empty(differentChannelPeer.Sent);
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

    private static byte[] BuildUIntPayload(uint value)
    {
        var writer = new PacketWriter();
        writer.Write(value);
        return writer.ToBytes();
    }

    private static IReadOnlyList<uint> ReadSelectMapIds(byte[] payload)
    {
        const int SelectMapPaddingSize = 105;

        var reader = new PacketReader(payload);
        var count = reader.ReadUInt();
        var mapIds = new List<uint>((int)count);
        for (var index = 0; index < count; index++)
        {
            mapIds.Add(reader.ReadUInt());
            reader.ReadBytes(SelectMapPaddingSize);
        }

        return mapIds;
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
