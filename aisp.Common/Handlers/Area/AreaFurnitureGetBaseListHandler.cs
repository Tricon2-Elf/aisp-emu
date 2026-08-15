using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaFurnitureGetBaseListHandler(IMyRoomRepository myRoomRepository)
    : PacketHandlerBase<FurnitureGetBaseListRequest, FurnitureGetBaseListResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.FurnitureGetBaseListRequest;

    public override PacketType ResponseType => PacketType.FurnitureGetBaseListResponse;

    public override ServerType ServerType => ServerType.Area;

    public override async Task<FurnitureGetBaseListResponse?> HandleAsync(
        FurnitureGetBaseListRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var catalog = await myRoomRepository.GetFurnitureCatalogAsync(ct);
        return new FurnitureGetBaseListResponse(
            0,
            catalog
                .Select(x => new FurnitureBaseEntry(
                    checked((uint)x.ItemId),
                    x.PlacementFlags,
                    x.Type
                ))
                .ToArray()
        );
    }
}
