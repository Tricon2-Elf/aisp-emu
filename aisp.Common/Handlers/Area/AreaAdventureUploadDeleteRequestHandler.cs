using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// Taking a disc off sale from the upload window. Result 0 makes the client drop it from its upload list and
/// clears the work's Uploaded flag on the server. The open window never redraws its left column from an
/// unsolicited work list (the client stores that reply without rendering it, and it can even release the
/// window's wait early), so the work is put back in front of the player by re-sending
/// recv_adventure_upload_started for the clerk: that runs the window's open sequence, and the client re-requests
/// and rebuilds both lists itself.
/// </summary>
public sealed class AreaAdventureUploadDeleteRequestHandler(
    IAdventureShopRepository shop,
    INpcRepository npcRepository
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AdventureUploadDeleteRequestRequest;
    public PacketType ResponseType => PacketType.AdventureUploadDeleteRequestResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = AdventureScriptIdRequest.FromBytes(payload.Span);
        var userId = session.User?.Id ?? session.UserId;
        var removed = await shop.DelistAsync(userId, request.ScriptId, ct);
        await session.SendAsync(
            ResponseType,
            new AdventureScriptIdResponse(removed ? 0u : 1u, request.ScriptId).ToBytes(),
            ct
        );
        if (!removed)
            return;
        var npcs = await npcRepository.GetActiveByMapAsync(session.MapId, session.ChannelId, ct);
        var clerk = npcs.FirstOrDefault(n =>
            n.InteractionType == NpcInteractionType.AdventureShopUpload && n.IsEnabled
        );
        if (clerk is null)
            return;
        await session.SendAsync(
            PacketType.AdventureUploadStartedNotify,
            new AdventureUploadStartedNotify(checked((uint)clerk.NpcObjectId), 0).ToBytes(),
            ct
        );
    }
}
