using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaNicotvSetChannelHandler(
    INicotvRepository nicotvRepository,
    SharedState state
)
    : PacketHandlerBase<NicotvSetChannelRequest, NicotvSetChannelResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.NicotvSetChannelRequest;
    public override PacketType ResponseType => PacketType.NicotvSetChannelResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<NicotvSetChannelResponse?> HandleAsync(
        NicotvSetChannelRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (
            !MyRoomInfo.IsMyRoomMap(session.MapId)
            || session.MyRoomId == 0
            || session.MyRoomId > int.MaxValue
        )
            return new NicotvSetChannelResponse(0, 0);

        var nicotv = await nicotvRepository.SetChannelAsync(
            checked((int)session.MyRoomId),
            request.NicotvId,
            request.ChannelId,
            ct
        );
        if (nicotv is null)
            return new NicotvSetChannelResponse(0, 0);

        await MyRoomFurnitureNotification.BroadcastToRoomAsync(
            state,
            session,
            session.MyRoomId,
            PacketType.NotifyNicotvSetChannel,
            new NotifyNicotvSetChannel(request.NicotvId, request.ChannelId).ToBytes(),
            includeSource: false,
            ct
        );
        return new NicotvSetChannelResponse(request.NicotvId, request.ChannelId);
    }
}
