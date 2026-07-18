using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Game.ServerScripts;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

public class AreaMyRoomSystemActorTests
{
    [Theory]
    [InlineData(MyRoomInfo.SixTatamiMapId, 73f, -170f)]
    [InlineData(MyRoomInfo.EightTatamiMapId, 123f, -170f)]
    [InlineData(MyRoomInfo.TenTatamiMapId, 123f, -220f)]
    [InlineData(MyRoomInfo.TwelveTatamiMapId, 173f, -220f)]
    public async Task NpcGetData_SendsNativeDoorAndWardrobeForEveryRoomSize(uint mapId, float entranceX, float entranceZ)
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var handler = new AreaNpcGetDataHandler(new NpcRepository(db));
            var session = new CapturingPlayerSession { MapId = mapId };

            await handler.HandleAsync(ReadOnlyMemory<byte>.Empty, session, TestContext.Current.CancellationToken);

            Assert.Equal(PacketType.NpcGetDataResponse, session.Sent[0].Type);
            var actors = session.Sent.Where(packet => packet.Type == PacketType.NpcNotifyData).Select(packet => ReadActor(packet.Payload)).ToDictionary(actor => actor.ObjectId);
            Assert.Equal(2, actors.Count);

            var door = actors[MyRoomSystemActors.DoorObjectId];
            Assert.Equal(MyRoomSystemActors.DoorObjectId, door.SlotId);
            Assert.Equal(MyRoomSystemActors.DoorModelId, door.ModelId);
            Assert.Equal(entranceX, door.X);
            Assert.Equal(0f, door.Y);
            Assert.Equal(entranceZ, door.Z);
            Assert.Equal(0, door.Rotation);

            var wardrobe = actors[MyRoomSystemActors.WardrobeObjectId];
            Assert.Equal(MyRoomSystemActors.WardrobeObjectId, wardrobe.SlotId);
            Assert.Equal(MyRoomSystemActors.WardrobeModelId, wardrobe.ModelId);
            Assert.Equal(-entranceX, wardrobe.X);
            Assert.Equal(0f, wardrobe.Y);
            Assert.Equal(entranceZ, wardrobe.Z);
            Assert.Equal(0, wardrobe.Rotation);
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
            var dispatcher = CreateDispatcher(db);
            var accessHandler = new AreaEventAccessNpcHandler(new NpcRepository(db), new ShopRepository(db), dispatcher, NullLogger<AreaEventAccessNpcHandler>.Instance);
            var selectHandler = new AreaEventSelectExecRHandler(dispatcher, NullLogger<AreaEventSelectExecRHandler>.Instance);
            var session = new CapturingPlayerSession
            {
                MapId = MyRoomInfo.BaseMapId,
                CharacterId = 42,
                User = new User { AiPoints = 12_345 },
            };

            await accessHandler.HandleAsync(BuildEventAccessNpcPayload(MyRoomSystemActors.WardrobeObjectId), session, TestContext.Current.CancellationToken);

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
