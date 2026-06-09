using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaAvatarMoveRequestHandler(SharedState state, DirectMapLinkTransitionService directMapLinkTransitionService) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AvatarMoveRequest;
    public PacketType ResponseType => PacketType.AvatarNotifyMove;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        if (session.IsMapTransitionPending)
            return;

        var avatarMove = AvatarMove.FromBytes(payload.Span);
        if (avatarMove.Moves.Length == 0)
            return;

        var samples = new List<DirectMapLinkTransitionService.PositionSample>(avatarMove.Moves.Length + 1) { new(session.X, session.Z) };

        foreach (var movement in avatarMove.Moves)
        {
            samples.Add(new DirectMapLinkTransitionService.PositionSample(movement.X, movement.Z));
        }

        var lastMovement = avatarMove.Moves[^1];

        session.X = lastMovement.X;
        session.Y = lastMovement.Y;
        session.Z = lastMovement.Z;
        session.Rotation = lastMovement.Rotation;
        session.MovementTypeId = (int)lastMovement.Animation;

        if (await directMapLinkTransitionService.TryHandleMovementTriggerAsync(session, samples, ct))
            return;

        session.HasMovedSinceMapLoad = true;
        //logger.LogInformation("AvatarMoveRequestHandler: CharacterId='{CharacterId}', X='{X}', Y='{Y}', Z='{Z}', Rotation='{Rotation}'", session.CharacterId, session.X, session.Y, session.Z, session.Rotation);
        var notify = new AvatarNotifyMove(session.CharacterId, avatarMove.Moves).ToBytes();

        foreach (var other in state.GetAreaPeers(session))
        {
            _ = other.SendAsync(ResponseType, notify, ct);
        }
    }
}
