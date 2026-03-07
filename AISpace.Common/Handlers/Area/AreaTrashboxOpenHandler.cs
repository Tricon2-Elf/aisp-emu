using AISpace.Common.Network.Packets.Area;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaTrashboxOpenHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.TrashboxOpenRequest;
    public PacketType ResponseType => PacketType.TrashboxOpenResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        // Request has 0 bytes. Response: result (4). recv_trashbox_open_r
        var response = new TrashboxOpenResponse(0); // 0 = success
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
