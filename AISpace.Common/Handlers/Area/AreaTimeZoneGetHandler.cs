using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaTimeZoneGetHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.TimeZoneGetRequest;
    public PacketType ResponseType => PacketType.TimeZoneGetResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var t = TimeZoneService.GetServerTime();
        var resp = new TimeZoneGetResponse(0, (uint)t.Phase, t.Current, t.Max, 1);
        await session.SendAsync(ResponseType, resp.ToBytes(), ct);
    }
}
