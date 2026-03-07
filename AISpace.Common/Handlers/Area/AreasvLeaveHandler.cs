using AISpace.Common.Network.Packets.Area;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreasvLeaveHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.AreasvLeaveRequest;

    public PacketType ResponseType => PacketType.AreasvLeaveResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var response = new AreasvLeaveResponse();
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
