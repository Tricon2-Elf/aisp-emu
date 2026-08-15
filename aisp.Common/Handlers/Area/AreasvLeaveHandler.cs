using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreasvLeaveHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AreasvLeaveRequest;

    public PacketType ResponseType => PacketType.AreasvLeaveResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var response = new AreasvLeaveResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
