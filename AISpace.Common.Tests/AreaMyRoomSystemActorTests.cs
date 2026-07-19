using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Game.ServerScripts;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

public class AreaMyRoomSystemActorTests
{
    private const uint DoorModelId = 8_000_990;
    private const uint WardrobeModelId = 8_000_030;

    private static readonly (uint MapId, uint DoorObjectId, float DoorX, float DoorZ, uint WardrobeObjectId, float WardrobeX, float WardrobeZ)[] RoomActorDefs =
    [
        (MyRoomInfo.SixTatamiMapId, 0x5FFF_FF01, 80f, -258f, 0x5FFF_FF02, -73f, -197f),
        (MyRoomInfo.EightTatamiMapId, 0x5FFF_FF11, 130f, -258f, 0x5FFF_FF12, -123f, -197f),
        (MyRoomInfo.TenTatamiMapId, 0x5FFF_FF21, 130f, -308f, 0x5FFF_FF22, -123f, -247f),
        (MyRoomInfo.TwelveTatamiMapId, 0x5FFF_FF31, 180f, -308f, 0x5FFF_FF32, -173f, -247f),
    ];

    public static TheoryData<uint, uint, float, float, uint, float, float> RoomActors
    {
        get
        {
            var data = new TheoryData<uint, uint, float, float, uint, float, float>();
            foreach (var row in RoomActorDefs)
                data.Add(row.MapId, row.DoorObjectId, row.DoorX, row.DoorZ, row.WardrobeObjectId, row.WardrobeX, row.WardrobeZ);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(RoomActors))]
    public async Task NpcGetData_SendsNativeDoorAndWardrobeForEveryRoomSize(uint mapId, uint doorObjectId, float doorX, float doorZ, uint wardrobeObjectId, float wardrobeX, float wardrobeZ)
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            await SeedMyRoomActorsAsync(db);
            var handler = new AreaNpcGetDataHandler(new NpcRepository(db));
            var session = new CapturingPlayerSession { MapId = mapId };

            await handler.HandleAsync(ReadOnlyMemory<byte>.Empty, session, TestContext.Current.CancellationToken);

            Assert.Equal(PacketType.NpcGetDataResponse, session.Sent[0].Type);
            var actors = session.Sent.Where(packet => packet.Type == PacketType.NpcNotifyData).Select(packet => ReadActor(packet.Payload)).ToDictionary(actor => actor.ObjectId);
            Assert.Equal(2, actors.Count);

            var door = actors[doorObjectId];
            Assert.Equal(doorObjectId, door.SlotId);
            Assert.Equal(DoorModelId, door.ModelId);
            Assert.Equal(doorX, door.X);
            Assert.Equal(0f, door.Y);
            Assert.Equal(doorZ, door.Z);
            Assert.Equal(0, door.Rotation);

            var wardrobe = actors[wardrobeObjectId];
            Assert.Equal(wardrobeObjectId, wardrobe.SlotId);
            Assert.Equal(WardrobeModelId, wardrobe.ModelId);
            Assert.Equal(wardrobeX, wardrobe.X);
            Assert.Equal(0f, wardrobe.Y);
            Assert.Equal(wardrobeZ, wardrobe.Z);
            Assert.Equal(0, wardrobe.Rotation);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task DoorAccess_StartsSysEvent002ClientScript()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            await SeedMyRoomActorsAsync(db);
            var dispatcher = CreateDispatcher(db);
            var accessHandler = new AreaEventAccessNpcHandler(new NpcRepository(db), new ShopRepository(db), dispatcher, NullLogger<AreaEventAccessNpcHandler>.Instance);
            var session = new CapturingPlayerSession { MapId = MyRoomInfo.BaseMapId, CharacterId = 42 };

            await accessHandler.HandleAsync(BuildEventAccessNpcPayload(0x5FFF_FF01), session, TestContext.Current.CancellationToken);

            Assert.Equal(ScriptedEvents.Keys.SysEvent002, session.ActiveEventKey);
            Assert.Equal(NpcEventKind.ClientScript, session.ActiveEventKind);
            Assert.Equal(EventCompletionPolicy.Replayable, session.ActiveEventCompletionPolicy);
            Assert.Equal(0u, ReadResult(session.Sent.Single(packet => packet.Type == PacketType.EventAccessNpcResponse).Payload));
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventStartNotify);
            var scriptPlay = Assert.Single(session.Sent, packet => packet.Type == PacketType.EventScriptPlayNotify);
            Assert.Equal(new EventScriptPlayNotify(ScriptedEvents.GetScriptLabel(ScriptedEvents.Keys.SysEvent002)).ToBytes(), scriptPlay.Payload);
            Assert.Equal("./script/sys_event/002.csv", ScriptedEvents.GetScriptLabel(ScriptedEvents.Keys.SysEvent002));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task WardrobeSelection_OpensStorageWithCurrentAiPointBalance()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            await SeedMyRoomActorsAsync(db);
            var dispatcher = CreateDispatcher(db);
            var accessHandler = new AreaEventAccessNpcHandler(new NpcRepository(db), new ShopRepository(db), dispatcher, NullLogger<AreaEventAccessNpcHandler>.Instance);
            var selectHandler = new AreaEventSelectExecRHandler(dispatcher, NullLogger<AreaEventSelectExecRHandler>.Instance);
            var session = new CapturingPlayerSession
            {
                MapId = MyRoomInfo.BaseMapId,
                CharacterId = 42,
                User = new User { AiPoints = 12_345 },
            };

            await accessHandler.HandleAsync(BuildEventAccessNpcPayload(0x5FFF_FF02), session, TestContext.Current.CancellationToken);

            Assert.Equal(ServerEvents.Keys.MyRoomWardrobe, session.ActiveEventKey);
            Assert.Equal(NpcEventKind.ServerScript, session.ActiveEventKind);
            Assert.Equal(EventCompletionPolicy.Replayable, session.ActiveEventCompletionPolicy);
            Assert.Equal(0u, ReadResult(session.Sent.Single(packet => packet.Type == PacketType.EventAccessNpcResponse).Payload));
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventStartNotify);
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventSelectInitNotify);
            var optionsPackets = session.Sent.Where(packet => packet.Type == PacketType.EventSelectPushNotify).ToArray();
            Assert.Equal(2, optionsPackets.Length);
            Assert.Equal("倉庫を利用する", new PacketReader(optionsPackets[0].Payload).ReadString("utf-8"));
            Assert.Equal("使用しない", new PacketReader(optionsPackets[1].Payload).ReadString("utf-8"));

            await selectHandler.HandleAsync(BuildEventSelectionPayload(0), session, TestContext.Current.CancellationToken);

            Assert.Null(session.ActiveEventKey);
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventEndNotify);
            var storage = Assert.Single(session.Sent, packet => packet.Type == PacketType.StorageOpenedNotify);
            Assert.Equal(12_345ul, new PacketReader(storage.Payload).ReadULong());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task MyRoomFurnitureResponse_DoesNotInjectBuiltinFixtures()
    {
        var handler = new AreaMyRoomGetFurnitureHandler();
        var session = new CapturingPlayerSession { MapId = MyRoomInfo.BaseMapId, CharacterId = 42 };

        await handler.HandleAsync(ReadOnlyMemory<byte>.Empty, session, TestContext.Current.CancellationToken);

        var response = Assert.Single(session.Sent);
        Assert.Equal(PacketType.MyRoomGetFurnitureResponse, response.Type);
        Assert.DoesNotContain(session.Sent, packet => packet.Type == PacketType.MyRoomNotifyFurniture);
    }

    private static async Task SeedMyRoomActorsAsync(MainContext db)
    {
        foreach (var row in RoomActorDefs)
        {
            db.Npcs.Add(CreateActor(row.MapId, row.DoorObjectId, DoorModelId, row.DoorX, row.DoorZ, ScriptedEvents.Keys.SysEvent002, NpcEventKind.ClientScript));
            db.Npcs.Add(CreateActor(row.MapId, row.WardrobeObjectId, WardrobeModelId, row.WardrobeX, row.WardrobeZ, ServerEvents.Keys.MyRoomWardrobe, NpcEventKind.ServerScript));
        }

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Npc CreateActor(uint mapId, uint objectId, uint modelId, float x, float z, string? eventKey = null, NpcEventKind eventKind = NpcEventKind.None) =>
        new()
        {
            MapId = mapId,
            ChannelId = -1,
            DayPhase = -1,
            DateStartUtc = DateTime.UnixEpoch,
            DateEndUtc = DateTime.MaxValue,
            NpcObjectId = objectId,
            ModelId = modelId,
            Name = string.Empty,
            X = x,
            Y = 0f,
            Z = z,
            Rotation = 0,
            InteractionType = NpcInteractionType.Decorative,
            EventKind = eventKey is null ? NpcEventKind.None : eventKind,
            EventKey = eventKey,
            IsEnabled = true,
        };

    private static ServerScriptDispatcher CreateDispatcher(MainContext db)
    {
        var serverScriptSession = new ServerScriptSession(new CharacterEventRepository(db), NullLogger<ServerScriptSession>.Instance);
        var wardrobeScript = new MyRoomWardrobeServerScript(serverScriptSession);
        return new ServerScriptDispatcher([wardrobeScript], serverScriptSession, NullLogger<ServerScriptDispatcher>.Instance);
    }

    private static byte[] BuildEventAccessNpcPayload(uint objectId)
    {
        var writer = new PacketWriter();
        writer.Write(objectId);
        writer.Write(0f);
        writer.Write(0f);
        writer.Write(0f);
        return writer.ToBytes();
    }

    private static byte[] BuildEventSelectionPayload(byte selection)
    {
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.Write(selection);
        return writer.ToBytes();
    }

    private static uint ReadResult(byte[] payload) => new PacketReader(payload).ReadUInt();

    private static ActorPacket ReadActor(byte[] payload)
    {
        var reader = new PacketReader(payload);
        Assert.Equal(0u, reader.ReadUInt());
        var objectId = reader.ReadUInt();
        var slotId = reader.ReadUInt();
        var modelId = reader.ReadUInt();
        Assert.Equal(string.Empty, reader.ReadFixedString(37));
        reader.ReadBytes(19);
        reader.ReadUInt();
        reader.ReadFloat();
        reader.ReadFloat();
        reader.ReadFloat();
        reader.ReadFloat();
        var x = reader.ReadFloat();
        var y = reader.ReadFloat();
        var z = reader.ReadFloat();
        var rotation = reader.ReadSByte();
        return new ActorPacket(objectId, slotId, modelId, x, y, z, rotation);
    }

    private sealed record ActorPacket(uint ObjectId, uint SlotId, uint ModelId, float X, float Y, float Z, sbyte Rotation);
}
