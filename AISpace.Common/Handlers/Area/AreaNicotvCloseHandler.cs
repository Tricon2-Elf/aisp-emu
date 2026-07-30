using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaNicotvCloseHandler(INicotvRepository nicotvRepository, SharedState state)
    : PacketHandlerBase<NicotvCloseRequest, NicotvCloseResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.NicotvCloseRequest;
    public override PacketType ResponseType => PacketType.NicotvCloseResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<NicotvCloseResponse?> HandleAsync(
        NicotvCloseRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (
            !MyRoomInfo.IsMyRoomMap(session.MapId)
            || session.MyRoomId == 0
            || session.MyRoomId > int.MaxValue
        )
            return new NicotvCloseResponse(1, request.NicotvId);

        var nicotv = await nicotvRepository.CloseAsync(
            checked((int)session.MyRoomId),
            request.NicotvId,
            ct
        );
        if (nicotv is null)
            return new NicotvCloseResponse(1, request.NicotvId);

        await MyRoomFurnitureNotification.BroadcastToRoomAsync(
            state,
            session,
            session.MyRoomId,
            PacketType.NotifyNicotvClose,
            new NotifyNicotvClose(request.NicotvId).ToBytes(),
            includeSource: false,
            ct
        );
        return new NicotvCloseResponse(0, request.NicotvId);
    }
}
