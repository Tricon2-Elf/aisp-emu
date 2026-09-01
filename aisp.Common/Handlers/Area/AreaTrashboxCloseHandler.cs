using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaTrashboxCloseHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.TrashboxCloseRequest;
    public PacketType ResponseType => PacketType.TrashboxCloseResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        // The client tears the bin window down without inspecting the result; 0 matches the other _r packets.
        var response = new TrashboxCloseResponse(0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
