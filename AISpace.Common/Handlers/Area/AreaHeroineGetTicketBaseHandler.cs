using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaHeroineGetTicketBaseHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.HeroineGetTicketBaseRequest;

    public PacketType ResponseType => PacketType.HeroineGetTicketBaseResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new HeroineGetTicketBaseResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
