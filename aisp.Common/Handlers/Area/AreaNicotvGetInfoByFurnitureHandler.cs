using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaNicotvGetInfoByFurnitureHandler(INicotvRepository nicotvRepository)
    : PacketHandlerBase<NicotvGetInfoByFurnitureRequest, NicotvGetInfoByFurnitureResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.NicotvGetInfoByFurnitureRequest;
    public override PacketType ResponseType => PacketType.NicotvGetInfoByFurnitureResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<NicotvGetInfoByFurnitureResponse?> HandleAsync(
        NicotvGetInfoByFurnitureRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (
            !MyRoomInfo.IsMyRoomMap(session.MapId)
            || session.MyRoomId == 0
            || session.MyRoomId > int.MaxValue
        )
            return new NicotvGetInfoByFurnitureResponse(request.FurnitureId, 0, new NicotvData());

        var nicotv = await nicotvRepository.GetOrCreateForFurnitureAsync(
            checked((int)session.MyRoomId),
            request.FurnitureId,
            ct
        );
        return new NicotvGetInfoByFurnitureResponse(
            request.FurnitureId,
            nicotv is null ? 0u : checked((uint)nicotv.Id),
            nicotv is null ? new NicotvData() : NicotvMapper.ToPacket(nicotv)
        );
    }
}
