using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

/// <summary>
/// send_robo_furnact_end (0xE7BC) → broadcast recv_notify_robo_furnact_end (0xB45C).
/// No direct response.
/// </summary>
public sealed class AreaRoboFurnactEndHandler(IRoboRepository roboRepository, SharedState state)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboFurnactEndRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = RoboFurnactEndRequest.FromBytes(payload.Span);
        if (
            session.MyRoomId == 0
            || !session.AccompanyingRoboIds.Contains(request.RoboId)
            || await roboRepository.GetAsync(checked((int)session.CharacterId), request.RoboId, ct)
                is null
        )
            return;

        var notify = new NotifyRoboFurnactEnd(request.RoboId).ToBytes();
        await MyRoomFurnitureNotification.BroadcastToRoomAsync(
            state,
            session,
            session.MyRoomId,
            PacketType.NotifyRoboFurnactEnd,
            notify,
            includeSource: true,
            ct
        );
    }
}
