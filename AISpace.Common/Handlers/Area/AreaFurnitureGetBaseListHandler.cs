using AISpace.Network.Packets.Area;
using AISpace.Network;

using AISpace.Common.Game;

namespace AISpace.Common.Handlers.Area;

public class AreaFurnitureGetBaseListHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.FurnitureGetBaseListRequest;

    public PacketType ResponseType => PacketType.FurnitureGetBaseListResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new FurnitureGetBaseListResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
