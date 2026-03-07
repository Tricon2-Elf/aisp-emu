using AISpace.Network.Packets.Area;
using AISpace.Network;

using AISpace.Common.Game;

namespace AISpace.Common.Handlers.Area;

public class AreaMoneyDataGetHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.MoneyDataGetRequest;

    public PacketType ResponseType => PacketType.MoneyDataGetResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new MoneyDataGetResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
