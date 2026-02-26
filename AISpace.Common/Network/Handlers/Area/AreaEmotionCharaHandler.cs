using AISpace.Common.Network.Packets.Area;
using AISpace.Common.Game;

namespace AISpace.Common.Network.Handlers;

public class AreaEmotionCharaHandler(SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.EmotionCharaRequest;
    public PacketType ResponseType => PacketType.EmotionCharaResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var request = EmotionCharaRequest.FromBytes(payload.Span);
        
        // 1. Response to sender
        var response = new EmotionCharaResponse(connection.CharacterId, 0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);

        // 2. Broadcast to all players (including oneself) for sound and animation
        var notify = new NotifyEmotionChara(connection.CharacterId, request.EmotionId);
        byte[] data = notify.ToBytes();

        foreach (var other in state.AreaClients.Values)
        {
            await other.SendAsync(PacketType.NotifyEmotionChara, data, ct);
        }
    }
}