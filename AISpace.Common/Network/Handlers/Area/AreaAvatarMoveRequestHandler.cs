using AISpace.Common.Game;
using AISpace.Common.Network.Packets.Area;
using Microsoft.Extensions.Logging;
using NLog;

namespace AISpace.Common.Network.Handlers.Area;

public class AreaAvatarMoveRequestHandler(ILogger<AreaAvatarMoveRequestHandler> _logger, SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.AvatarMoveRequest;

    public PacketType ResponseType => PacketType.AvatarMoveRequest;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var avatarMove = AvatarMove.FromBytes(payload.Span);
        var movement = avatarMove.Moves[0];
        _logger.LogInformation($"X{movement.X:0} Y{movement.Y:0} Z{movement.Z:0} Rot{movement.Rotation:000} A{(byte)movement.Animation:0}");
        var notify = new AvatarNotifyMove(1, (uint)connection.User!.Characters.First().Id, movement).ToBytes();
        foreach (var other in state.AreaClients.Values)
        {
            if (other.Id == connection.Id)
                continue;
            await other.SendAsync(ResponseType, notify, ct);
        }
    }
}
