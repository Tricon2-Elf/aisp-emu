using AISpace.Network.Packets.Area;
using AISpace.Network;

using AISpace.Common.Game;

namespace AISpace.Common.Handlers.Area;

public class AreaHeroineGetTicketBaseHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.HeroineGetTicketBaseRequest;

    public PacketType ResponseType => PacketType.HeroineGetTicketBaseResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new HeroineGetTicketBaseResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
