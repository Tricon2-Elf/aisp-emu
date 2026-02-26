using AISpace.Common.Game;
using AISpace.Common.Network.Packets.Area;

namespace AISpace.Common.Network.Handlers.Area;

public class AreaAvatarMoveRequestHandler(SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.AvatarMoveRequest;
    public PacketType ResponseType => PacketType.AvatarNotifyMove;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
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

        connection.X = lastMovement.X;
        connection.Y = lastMovement.Y;
        connection.Z = lastMovement.Z;
        connection.Rotation = lastMovement.Rotation;
        connection.CurrentAnimation = lastMovement.Animation;

        var notify = new AvatarNotifyMove(1, connection.CharacterId, lastMovement).ToBytes();

        foreach (var other in state.AreaClients.Values)
        {
            if (other.Id == connection.Id)
                continue;
            _ = other.SendAsync(ResponseType, notify, ct);
        }
    }
}
