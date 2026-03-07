using AISpace.Common.Network.Packets.Area;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaNiconiCommonsBaseListHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.NiconiCommonsBaseListRequest;

    public PacketType ResponseType => PacketType.NiconiCommonsBaseListResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var response = new NiconiCommonsBaseListResponse();
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
