using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaNicotvGetPlayheadTimeRequestRHandler(
    INicotvRepository nicotvRepository,
    SharedState state
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.NicotvGetPlayheadTimeRequestRRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = NicotvGetPlayheadTimeRequestRRequest.FromBytes(payload.Span);
        if (
            !MyRoomInfo.IsMyRoomMap(session.MapId)
            || session.MyRoomId == 0
            || session.MyRoomId > int.MaxValue
            || request.RequestingUserId == 0
            || request.RequestingUserId > int.MaxValue
        )
            return;

        var nicotv = await nicotvRepository.GetByIdInRoomAsync(
            checked((int)session.MyRoomId),
            request.NicotvId,
            ct
        );
        if (nicotv is null)
            return;

        var requester = state.GetAreaSessionByUserId(
            checked((int)request.RequestingUserId),
            session.MapId,
            session.ChannelId
        );
        if (requester is null || requester.MyRoomId != session.MyRoomId)
            return;

        await requester.SendAsync(
            PacketType.NicotvGetPlayheadTimeResponse,
            new NicotvGetPlayheadTimeResponse(request.NicotvId, request.Seconds).ToBytes(),
            ct
        );

        // No client send_nicotv_set_playhead_time exists; peer-reported seek times are
        // broadcast so other occupants stay aligned with the active viewer.
        await MyRoomFurnitureNotification.BroadcastToRoomAsync(
            state,
            session,
            session.MyRoomId,
            PacketType.NotifyNicotvSetPlayheadTime,
            new NotifyNicotvSetPlayheadTime(request.NicotvId, request.Seconds).ToBytes(),
            includeSource: true,
            ct
        );
        await session.SendAsync(
            PacketType.NicotvSetPlayheadTimeResponse,
            new NicotvSetPlayheadTimeResponse(0, request.NicotvId).ToBytes(),
            ct
        );
    }
}
