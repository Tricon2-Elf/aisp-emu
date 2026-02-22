using AISpace.Common.Network.Packets.Area;
using AISpace.Common.Game;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Area;

public class AreaAvatarMoveRequestHandler(ILogger<AreaAvatarMoveRequestHandler> logger, SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.AvatarMoveRequest;
    public PacketType ResponseType => PacketType.AvatarNotifyMove;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var avatarMove = AvatarMove.FromBytes(payload.Span);
        if (avatarMove.Moves.Length == 0) return;

        var movement = avatarMove.Moves[^1]; // Берем последнюю точку

        // Сохраняем позицию, чтобы новые игроки видели его тут
        connection.X = movement.X;
        connection.Y = movement.Y;
        connection.Z = movement.Z;
        connection.Rotation = movement.Rotation;

        var notify = new AvatarNotifyMove(1, connection.CharacterId, movement).ToBytes();

        foreach (var other in state.AreaClients.Values)
        {
            if (other.Id == connection.Id) continue; 
            _ = other.SendAsync(ResponseType, notify, ct);
        }
    }
}