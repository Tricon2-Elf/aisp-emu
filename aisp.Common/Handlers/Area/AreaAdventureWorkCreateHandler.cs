using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>新規作成 in the drama editor: allocates the next work id and consumes manuscript sheets from the account's stock.</summary>
public sealed class AreaAdventureWorkCreateHandler(IAdventureWorkRepository works)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AdventureWorkCreateRequest;
    public PacketType ResponseType => PacketType.AdventureWorkCreateResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = AdventureWorkCreateRequest.FromBytes(payload.Span);
        var (work, stock) = await works.CreateAsync(
            session.User?.Id ?? session.UserId,
            (int)session.CharacterId,
            (int)Math.Min(request.Sheets, 1000),
            ct
        );
        if (work is null)
        {
            await session.SendAsync(
                ResponseType,
                new AdventureWorkCreateResponse(1, 0, 0).ToBytes(),
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
            new AdventureWorkCreateResponse(0, (uint)work.Sheets, (ushort)work.WorkId).ToBytes(),
            ct
        );
    }
}
