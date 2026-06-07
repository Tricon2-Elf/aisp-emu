using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Services;
using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class ItemGetBaseListHandler(IItemBaseListCache cache) : IPacketHandler
{
    public PacketType RequestType => PacketType.ItemGetBaseListRequest;
    public PacketType ResponseType => PacketType.ItemGetBaseListResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        if (cache.ResponsePayload.IsEmpty)
            await cache.WarmAsync(ct);

        await session.SendAsync(ResponseType, cache.ResponsePayload.ToArray(), ct);
    }
}
