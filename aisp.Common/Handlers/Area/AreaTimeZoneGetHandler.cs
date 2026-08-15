using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaTimeZoneGetHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.TimeZoneGetRequest;
    public PacketType ResponseType => PacketType.TimeZoneGetResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var t = TimeZoneService.GetServerTime();
        var resp = new TimeZoneGetResponse(0, (uint)t.Phase, t.Current, t.Max, 1);
        await session.SendAsync(ResponseType, resp.ToBytes(), ct);
    }
}
