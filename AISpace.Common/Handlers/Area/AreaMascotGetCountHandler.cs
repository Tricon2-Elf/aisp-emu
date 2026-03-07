using AISpace.Common.Network.Packets.Area;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaMascotGetCountHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.MascotGetCountRequest;

    public PacketType ResponseType => PacketType.MascotGetCountResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var response = new MascotGetCountResponse();
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
