using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// Drama disc shop window closed. Like the item shop, the client needs both the end reply and the
/// empty "ended" notify; with only the reply the window stays open.
/// </summary>
public sealed class AreaAdventureShopEndHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AdventureShopEndRequest;
    public PacketType ResponseType => PacketType.AdventureShopEndResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        await session.SendAsync(ResponseType, new AdventureShopEndResponse().ToBytes(), ct);
        await session.SendAsync(
            PacketType.AdventureShopEndedNotify,
            new AdventureShopEndedNotify().ToBytes(),
            ct
        );
    }
}
