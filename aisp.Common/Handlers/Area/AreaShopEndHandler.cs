using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public sealed class AreaShopEndHandler(ILogger<AreaShopEndHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ShopEndRequest;
    public PacketType ResponseType => PacketType.ShopEndResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        logger.LogInformation(
            "ShopEndRequest from {CharacterId} on map {MapId}",
            session.CharacterId,
            session.MapId
        );
        session.ActiveShopId = null;
        await session.SendAsync(ResponseType, new ShopEndResponse(0).ToBytes(), ct);
        await session.SendAsync(PacketType.ShopEndedNotify, new ShopEndedNotify().ToBytes(), ct);
    }
}
