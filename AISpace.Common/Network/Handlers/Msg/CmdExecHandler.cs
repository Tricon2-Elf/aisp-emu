using AISpace.Common.Network.Packets.Msg;
using AISpace.Common.Network.Packets.Area;
using AISpace.Common.Game;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Msg;

public class CmdExecHandler(SharedState state, ILogger<CmdExecHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.CmdExecRequest;
    public PacketType ResponseType => PacketType.CmdExecResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var request = CmdExecRequest.FromBytes(payload.Span);
        
        var response = new CmdExecResponse(request.MessageId, 0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);

        string cmd = request.Command.ToLower();
        logger.LogInformation($"[CMD] Player {connection.CharacterId} executed: /{cmd}");

        // логика кнопки escape
        if (cmd == "escape" || cmd == "reset")
        {
            connection.X = 0f;
            connection.Y = 0.1f;
            connection.Z = 0f;
            connection.Rotation = 0;

            var moveData = new MovementData(connection.X, connection.Y, connection.Z, connection.Rotation, MovementType.Stopped);
            var notify = new AvatarNotifyMove(1, connection.CharacterId, moveData).ToBytes();

            foreach (var client in state.AreaClients.Values)
            {
                await client.SendAsync(PacketType.AvatarNotifyMove, notify, ct);
            }
            
            logger.LogInformation($"[ESCAPE] Player {connection.CharacterId} teleported to start point.");
        }
    }
}
