using AISpace.Common.Network.Packets.Area;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaMoneyDataGetHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.MoneyDataGetRequest;

    public PacketType ResponseType => PacketType.MoneyDataGetResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var response = new MoneyDataGetResponse();
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
