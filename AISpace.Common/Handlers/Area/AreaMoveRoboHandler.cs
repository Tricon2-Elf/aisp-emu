using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaMoveRoboHandler(IRoboRepository roboRepository, SharedState state)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MoveRoboRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = MoveRoboRequest.FromBytes(payload.Span);
        var robo = await roboRepository.GetAsync(
            checked((int)session.CharacterId),
            request.RoboId,
            ct
        );
        if (robo is null || !session.AccompanyingRoboIds.Contains(request.RoboId))
            return;

        var notify = new AvatarNotifyMove(robo.Character.SlotId, request.Moves).ToBytes();
        foreach (var peer in state.GetAreaPeers(session))
            await peer.SendAsync(PacketType.AvatarNotifyMove, notify, ct);
    }
}
