using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaAdventureWorkDeleteHandler(IAdventureWorkRepository works)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AdventureWorkDeleteRequest;
    public PacketType ResponseType => PacketType.AdventureWorkDeleteResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = AdventureWorkDeleteRequest.FromBytes(payload.Span);
        var (removed, stock) = await works.DeleteAsync(
            session.User?.Id ?? session.UserId,
            request.WorkId,
            ct
        );
        await session.SendAsync(
            ResponseType,
            new AdventureWorkDeleteResponse(removed ? 0u : 1u, request.WorkId).ToBytes(),
            ct
        );
        if (removed)
            await session.SendAsync(
                PacketType.AdventureUpdatedSheetStackNotify,
                new AdventureUpdatedSheetStackNotify((uint)stock).ToBytes(),
                ct
            );
    }
}
