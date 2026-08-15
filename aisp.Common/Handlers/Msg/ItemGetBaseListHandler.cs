using aisp.Common.Game;
using aisp.Common.Services;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class ItemGetBaseListHandler(IItemBaseListCache cache) : IPacketHandler
{
    public PacketType RequestType => PacketType.ItemGetBaseListRequest;
    public PacketType ResponseType => PacketType.ItemGetBaseListResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var response = cache.GetResponsePayload(session.Language);
        if (response.IsEmpty)
            await cache.WarmAsync(ct);

        await session.SendAsync(
            ResponseType,
            cache.GetResponsePayload(session.Language).ToArray(),
            ct
        );
    }
}
