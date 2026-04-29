using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaMoneyDataGetHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.MoneyDataGetRequest;

    public PacketType ResponseType => PacketType.MoneyDataGetResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new MoneyDataGetResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
