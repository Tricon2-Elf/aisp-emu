using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaRoboGetListHandler(RoboInventoryStore roboStore) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboGetListRequest;

    public PacketType ResponseType => PacketType.RoboGetListResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var robos = roboStore.GetAll(session.CharacterId);
        await session.SendAsync(ResponseType, new RoboGetListResponse(robos).ToBytes(), ct);
    }
}
