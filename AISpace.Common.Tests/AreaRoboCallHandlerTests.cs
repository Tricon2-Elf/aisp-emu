using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

public class AreaRoboCallHandlerTests
{
    [Fact]
    public async Task HandleAsync_EchoesRoboIdAndKeepsRestingState()
    {
        var store = new RoboInventoryStore();
        var objectId = RoboInventoryStore.ObjectIdFor(1);
        var chara = new CharaData(objectId, 1002011, "Robot") { Visual = new CharaVisual(BloodType.A, 1, 1, 0, objectId, 0, 10930010) };
        store.Upsert(1, new RoboData(1, chara, state: 0));

        var handler = new AreaRoboCallHandler(store, NullLogger<AreaRoboCallHandler>.Instance);
        var session = new CapturingPlayerSession { CharacterId = 1 };
        var writer = new PacketWriter();
        writer.Write(1u);

        await handler.HandleAsync(writer.ToBytes(), session, TestContext.Current.CancellationToken);

        var sent = Assert.Single(session.Sent);
        Assert.Equal(PacketType.RoboCallResponse, sent.Type);
        var callReader = new PacketReader(sent.Payload);
        Assert.Equal(1u, callReader.ReadUInt());
        Assert.Equal(0u, callReader.ReadUInt());

        Assert.True(store.TryGet(1, 1, out var stored));
        Assert.Equal(0u, stored!.State);
    }
}
