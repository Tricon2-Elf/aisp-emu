using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// Drama playback start. send_adventure_start carries no body and nothing on the wire names the scenario's map:
/// before sending, the client parses the scenario's first CHANGEMAP and caches the resolved field in its
/// adventure manager. The server acknowledges and then routes the player to the drama stage map 30000000 with an
/// ordinary notify_change_map. The client's transition code special-cases that id and loads its cached field in
/// the visual-novel presentation instead, and the scenario's CAM_SET preset check then passes. Routing to the
/// scenario's real map instead leaves the world scene under the VN layer; a plain ack leaves the client waiting,
/// because CHANGEMAP only builds the stage while the current map is 30000000.
/// </summary>
public sealed class AreaAdventureStartHandler(
    DirectMapLinkTransitionService directMapLinkTransitionService,
    ILogger<AreaAdventureStartHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public const uint DramaStageMapId = 30_000_000;

    public PacketType RequestType => PacketType.AdventureStartRequest;
    public PacketType ResponseType => PacketType.AdventureStartResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        await session.SendAsync(ResponseType, new AdventureStartResponse().ToBytes(), ct);
        if (session.MapId == DramaStageMapId)
            return;
        session.AdventureReturnMapId = session.MapId;
        var moved = await directMapLinkTransitionService.TryTeleportToMapAsync(
            session,
            DramaStageMapId,
            ct
        );
        if (!moved)
        {
            session.AdventureReturnMapId = 0;
            logger.LogWarning(
                "adventure_start: could not route character {CharacterId} to the drama stage",
                session.CharacterId
            );
        }
    }
}
