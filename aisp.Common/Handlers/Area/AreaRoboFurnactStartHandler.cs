using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// send_robo_furnact_start (0x08F2) → broadcast recv_notify_robo_furnact_start (0xB77E).
/// No direct response (same pattern as MoveRobo).
/// </summary>
public sealed class AreaRoboFurnactStartHandler(
    IRoboRepository roboRepository,
    IMyRoomRepository myRoomRepository,
    SharedState state
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboFurnactStartRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = RoboFurnactStartRequest.FromBytes(payload.Span);
        if (
            session.MyRoomId == 0
            || !session.AccompanyingRoboIds.Contains(request.RoboId)
            || await roboRepository.GetAsync(checked((int)session.CharacterId), request.RoboId, ct)
                is null
        )
            return;

        var furniture = await myRoomRepository.GetFurnitureAsync(
            checked((int)session.MyRoomId),
            request.FurnitureId,
            ct
        );
        if (furniture is null)
            return;

        var notify = new NotifyRoboFurnactStart(
            request.RoboId,
            request.FurnitureId,
            request.Start
        ).ToBytes();
        await MyRoomFurnitureNotification.BroadcastToRoomAsync(
            state,
            session,
            session.MyRoomId,
            PacketType.NotifyRoboFurnactStart,
            notify,
            includeSource: true,
            ct
        );
    }
}
