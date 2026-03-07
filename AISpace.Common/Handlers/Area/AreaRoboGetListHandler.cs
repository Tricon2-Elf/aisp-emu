using AISpace.Network.Packets.Area;
using AISpace.Network;

using AISpace.Common.Game;

namespace AISpace.Common.Handlers.Area;

public class AreaRoboGetListHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.RoboGetListRequest;

    public PacketType ResponseType => PacketType.RoboGetListResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new RoboGetListResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
