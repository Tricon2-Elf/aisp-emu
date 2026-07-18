using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaFurnitureGetBaseListHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    private const uint FlagYawSnap = 0x10;

    public PacketType RequestType => PacketType.FurnitureGetBaseListRequest;

    public PacketType ResponseType => PacketType.FurnitureGetBaseListResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var entries = new FurnitureBaseEntry[] { new(MyRoomInfo.ClosetItemId, FlagYawSnap, 0) };

        await session.SendAsync(ResponseType, new FurnitureGetBaseListResponse(0, entries).ToBytes(), ct);
    }
}
