using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaMascotGetCountHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MascotGetCountRequest;

    public PacketType ResponseType => PacketType.MascotGetCountResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var response = new MascotGetCountResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
