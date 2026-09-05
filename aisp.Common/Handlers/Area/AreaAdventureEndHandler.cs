using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>Drama playback end: acknowledge and route the player back to the map they started from.</summary>
public sealed class AreaAdventureEndHandler(
    DirectMapLinkTransitionService directMapLinkTransitionService
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AdventureEndRequest;
    public PacketType ResponseType => PacketType.AdventureEndResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        await session.SendAsync(ResponseType, new AdventureEndResponse().ToBytes(), ct);
        var returnMap = session.AdventureReturnMapId;
        session.AdventureReturnMapId = 0;
        if (returnMap != 0 && returnMap != session.MapId)
            await directMapLinkTransitionService.TryTeleportToMapAsync(session, returnMap, ct);
    }
}
