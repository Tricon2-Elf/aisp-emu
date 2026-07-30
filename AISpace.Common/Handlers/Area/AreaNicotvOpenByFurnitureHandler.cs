using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaNicotvOpenByFurnitureHandler(INicotvRepository nicotvRepository)
    : PacketHandlerBase<NicotvOpenByFurnitureRequest, NicotvOpenResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.NicotvOpenByFurnitureRequest;
    public override PacketType ResponseType => PacketType.NicotvOpenResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<NicotvOpenResponse?> HandleAsync(
        NicotvOpenByFurnitureRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (
            !MyRoomInfo.IsMyRoomMap(session.MapId)
            || session.MyRoomId == 0
            || session.MyRoomId > int.MaxValue
        )
            return new NicotvOpenResponse(request.FurnitureId, 0, new NicotvData());

        var nicotv = await nicotvRepository.UpdateForFurnitureAsync(
            checked((int)session.MyRoomId),
            request.FurnitureId,
            request.Nicotv,
            ct
        );
        return new NicotvOpenResponse(
            request.FurnitureId,
            nicotv is null ? 0u : checked((uint)nicotv.Id),
            nicotv is null ? new NicotvData() : NicotvMapper.ToPacket(nicotv)
        );
    }
}
