using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// Commons shop window closed. Like the item and drama shops, the client needs both the end
/// reply and the empty "ended" notify; with only the reply the window stays open.
/// </summary>
public sealed class AreaNiconiCommonsShopEndHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.NiconiCommonsShopEndRequest;
    public PacketType ResponseType => PacketType.NiconiCommonsShopEndResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        session.ActiveShopId = null;
        await session.SendAsync(ResponseType, new NiconiCommonsShopEndResponse().ToBytes(), ct);
        await session.SendAsync(
            PacketType.NiconiCommonsShopEndedNotify,
            new NiconiCommonsShopEndedNotify().ToBytes(),
            ct
        );
    }
}
