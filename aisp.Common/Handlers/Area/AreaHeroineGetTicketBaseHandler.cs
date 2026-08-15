using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaHeroineGetTicketBaseHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.HeroineGetTicketBaseRequest;

    public PacketType ResponseType => PacketType.HeroineGetTicketBaseResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var response = new HeroineGetTicketBaseResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
