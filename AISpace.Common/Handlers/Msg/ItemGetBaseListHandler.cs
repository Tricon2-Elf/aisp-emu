using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class ItemGetBaseListHandler(IItemRepository itemRepo) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ItemGetBaseListRequest;
    public PacketType ResponseType => PacketType.ItemGetBaseListResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var rows = await itemRepo.GetAllAsync(ct);
        var items = rows.Select(ItemEntityMapper.ToItemBaseListData).ToList();
        var response = new ItemGetBaseListResponse(0, items);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
