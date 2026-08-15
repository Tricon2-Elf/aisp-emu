using aisp.Common.Game;
using aisp.Common.Services;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaAvatarMoveRequestHandler(
    SharedState state,
    DirectMapLinkTransitionService directMapLinkTransitionService,
    ScriptedEventTriggerService scriptedEventTriggerService
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AvatarMoveRequest;
    public PacketType ResponseType => PacketType.AvatarNotifyMove;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.IsMapTransitionPending)
            return;

        var avatarMove = AvatarMove.FromBytes(payload.Span);
        if (avatarMove.Moves.Length == 0)
            return;

        var samples = new List<DirectMapLinkTransitionService.PositionSample>(
            avatarMove.Moves.Length + 1
        )
        {
            new(session.X, session.Z),
        };
        var scriptedEventSamples = new List<MovementPositionSample>(avatarMove.Moves.Length + 1)
        {
            new(session.X, session.Y, session.Z),
        };

        foreach (var movement in avatarMove.Moves)
        {
            samples.Add(new DirectMapLinkTransitionService.PositionSample(movement.X, movement.Z));
            scriptedEventSamples.Add(
                new MovementPositionSample(movement.X, movement.Y, movement.Z)
            );
        }

        var lastMovement = avatarMove.Moves[^1];

        session.X = lastMovement.X;
        session.Y = lastMovement.Y;
        session.Z = lastMovement.Z;
        session.Rotation = lastMovement.Rotation;
        session.MovementTypeId = (int)lastMovement.Animation;

        if (
            await directMapLinkTransitionService.TryHandleMovementTriggerAsync(session, samples, ct)
        )
            return;

        if (
            await scriptedEventTriggerService.TryStartOnMovementAsync(
                session,
                scriptedEventSamples,
                ct
            )
        )
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
