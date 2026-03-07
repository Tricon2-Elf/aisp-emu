using AISpace.Network;
using AISpace.Network.Packets.Area;

using AISpace.Common.Game;

namespace AISpace.Common.Handlers.Area;

public class AreaEmotionCharaHandler(SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.EmotionCharaRequest;
    public PacketType ResponseType => PacketType.EmotionCharaResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = EmotionCharaRequest.FromBytes(payload.Span);

        // 1. Response to sender
        var response = new EmotionCharaResponse(session.CharacterId, 0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);

        // 2. Broadcast to all players (including oneself) for sound and animation
        var notify = new NotifyEmotionChara(session.CharacterId, request.EmotionId);
        byte[] data = notify.ToBytes();

        foreach (var other in state.AreaClients.Values)
        {
            await other.SendAsync(PacketType.NotifyEmotionChara, data, ct);
        }
    }
}
