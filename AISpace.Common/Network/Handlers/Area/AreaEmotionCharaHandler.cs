using AISpace.Common.Game;
using AISpace.Common.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers;

public class AreaEmotionCharaHandler(ILogger<AreaEmotionCharaHandler> logger, SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.EmotionCharaRequest;
    public PacketType ResponseType => PacketType.EmotionCharaResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var request = EmotionCharaRequest.FromBytes(payload.Span);
        var response = new EmotionCharaResponse(request.ObjId, 0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
        var notify = new NotifyEmotionChara(request.ObjId, request.EmotionId);
        logger.LogInformation("Sending NotifyEmotionChara to all clients: {ObjId} {EmotionId}", request.ObjId, request.EmotionId);
        foreach (var client in state.AreaClients.Values)
        {
            await client.SendAsync(PacketType.NotifyEmotionChara, notify.ToBytes(), ct);
        }
    }
}
