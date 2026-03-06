using AISpace.Common.Game;
using AISpace.Common.Network.Packets;

namespace AISpace.Common.Network.Handlers;

public class AreaTimeZoneGetHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.TimeZoneGetRequest;
    public PacketType ResponseType => PacketType.TimeZoneGetResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var t = TimeZoneService.GetServerTime();
        var resp = new TimeZoneGetResponse(0, (uint)t.Phase, t.Current, t.Max, 1);
        await connection.SendAsync(ResponseType, resp.ToBytes(), ct);
    }
}
