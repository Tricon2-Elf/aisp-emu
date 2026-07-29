using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaFurnitureGetBaseListHandler(IMyRoomRepository myRoomRepository) : PacketHandlerBase<FurnitureGetBaseListRequest, FurnitureGetBaseListResponse>, IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.FurnitureGetBaseListRequest;

    public override PacketType ResponseType => PacketType.FurnitureGetBaseListResponse;

    public override ServerType ServerType => ServerType.Area;

    public override async Task<FurnitureGetBaseListResponse?> HandleAsync(FurnitureGetBaseListRequest request, IPlayerSession session, CancellationToken ct = default)
    {
        var catalog = await myRoomRepository.GetFurnitureCatalogAsync(ct);
        return new FurnitureGetBaseListResponse(0, catalog.Select(x => new FurnitureBaseEntry(checked((uint)x.ItemId), x.PlacementFlags, x.Type)).ToArray());
    }
}
