using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaEmotionCharaHandler(SharedState state) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EmotionCharaRequest;
    public PacketType ResponseType => PacketType.EmotionCharaResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = EmotionCharaRequest.FromBytes(payload.Span);

        // 1. Response to sender
        var response = new EmotionCharaResponse(session.CharacterId, 0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);

        // 2. Broadcast to peers on the same map/channel (including oneself) for sound and animation
        var notify = new NotifyEmotionChara(session.CharacterId, request.EmotionId);
        byte[] data = notify.ToBytes();

        foreach (var other in state.GetAreaPeers(session, includeSelf: true))
        {
            await other.SendAsync(PacketType.NotifyEmotionChara, data, ct);
        }
    }
}
