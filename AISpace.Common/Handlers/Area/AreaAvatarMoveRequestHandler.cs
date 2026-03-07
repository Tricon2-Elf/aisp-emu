using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaAvatarMoveRequestHandler(SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.AvatarMoveRequest;
    public PacketType ResponseType => PacketType.AvatarNotifyMove;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var avatarMove = AvatarMove.FromBytes(payload.Span);
        if (avatarMove.Moves.Length == 0)
            return;

        var lastMovement = avatarMove.Moves[^1];

        byte maxAnimation = 0;
        foreach (var m in avatarMove.Moves)
        {
            if ((byte)m.Animation > maxAnimation)
            {
                maxAnimation = (byte)m.Animation;
            }
        }

        lastMovement.Animation = (MovementType)maxAnimation;

        session.X = lastMovement.X;
        session.Y = lastMovement.Y;
        session.Z = lastMovement.Z;
        session.Rotation = lastMovement.Rotation;
        session.MovementTypeId = (int)lastMovement.Animation;

        var notify = new AvatarNotifyMove(1, session.CharacterId, lastMovement).ToBytes();

        foreach (var other in state.AreaClients.Values)
        {
            if (other.ConnectionId == session.ConnectionId)
                continue;
            _ = other.SendAsync(ResponseType, notify, ct);
        }
    }
}
