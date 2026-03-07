using AISpace.Common.Network.Packets;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaHeroineGetTicketBaseHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.HeroineGetTicketBaseRequest;

    public PacketType ResponseType => PacketType.HeroineGetTicketBaseResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var response = new HeroineGetTicketBaseResponse();
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
