using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>Returns sheets from a work to the account stock. The reply carries the applied delta, which the client adds to its local count.</summary>
public sealed class AreaAdventureWorkSubSheetHandler(IAdventureWorkRepository works)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AdventureWorkSubSheetRequest;
    public PacketType ResponseType => PacketType.AdventureWorkSubSheetResponse;
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
            -delta,
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
        // Stock push before the reply: the client refreshes its stock display in the tick the reply arrives.
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
