using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaTrashboxOpenHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.TrashboxOpenRequest;
    public PacketType ResponseType => PacketType.TrashboxOpenResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        // Request has 0 bytes. Response: result (4). recv_trashbox_open_r
        var response = new TrashboxOpenResponse(0); // 0 = success
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
