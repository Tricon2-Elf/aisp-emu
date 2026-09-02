using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>Takes sheets from the account stock into a work. The reply carries the applied delta, which the client adds to its local count.</summary>
public sealed class AreaAdventureWorkAddSheetHandler(IAdventureWorkRepository works)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AdventureWorkAddSheetRequest;
    public PacketType ResponseType => PacketType.AdventureWorkAddSheetResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = AdventureWorkSheetRequest.FromBytes(payload.Span);
        var delta = (int)Math.Min(request.Count, 1000);
        var (work, stock) = await works.AdjustSheetsAsync(
            session.User?.Id ?? session.UserId,
            request.WorkId,
            +delta,
            ct
        );
        if (work is null)
        {
            await session.SendAsync(
                ResponseType,
                new AdventureWorkSheetResponse(1, request.WorkId, 0).ToBytes(),
                ct
            );
            return;
        }
        // Stock push before the reply: recv only stores CAdvMgr+0x1BC. The editor caption is 1BC-1C0 and
        // paints on the next local add/remove sheet, not on this recv.
        await session.SendAsync(
            PacketType.AdventureUpdatedSheetStackNotify,
            new AdventureUpdatedSheetStackNotify((uint)stock).ToBytes(),
            ct
        );
        await session.SendAsync(
            ResponseType,
            new AdventureWorkSheetResponse(0, (ushort)work.WorkId, (uint)delta).ToBytes(),
            ct
        );
    }
}
