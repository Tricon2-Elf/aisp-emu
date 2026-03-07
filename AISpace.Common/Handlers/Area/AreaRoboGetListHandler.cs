using AISpace.Common.Network.Packets.Area;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaRoboGetListHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.RoboGetListRequest;

    public PacketType ResponseType => PacketType.RoboGetListResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var response = new RoboGetListResponse();
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
