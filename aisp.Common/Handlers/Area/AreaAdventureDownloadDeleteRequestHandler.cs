using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>Removing a disc from the download list. The purchase stays, so the 購入履歴 can still re-download it.</summary>
public sealed class AreaAdventureDownloadDeleteRequestHandler(IAdventureShopRepository shop)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AdventureDownloadDeleteRequestRequest;
    public PacketType ResponseType => PacketType.AdventureDownloadDeleteRequestResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = AdventureScriptIdRequest.FromBytes(payload.Span);
        await shop.HideDownloadAsync(session.User?.Id ?? session.UserId, request.ScriptId, ct);
        // Always 0: the client removes its local cache row either way, and a row the server never had is fine.
        await session.SendAsync(
            ResponseType,
            new AdventureScriptIdResponse(0, request.ScriptId).ToBytes(),
            ct
        );
    }
}
