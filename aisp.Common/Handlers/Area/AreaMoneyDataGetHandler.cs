using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaMoneyDataGetHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.MoneyDataGetRequest;

    public PacketType ResponseType => PacketType.MoneyDataGetResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var response = new MoneyDataGetResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
