using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaEmotionCharaHandler(SharedState state, IRoboRepository roboRepository)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EmotionCharaRequest;
    public PacketType ResponseType => PacketType.EmotionCharaResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = EmotionCharaRequest.FromBytes(payload.Span);
        var ownsTarget = request.ObjId == session.CharacterId;
        if (
            !ownsTarget
            && session.CharacterId != 0
            && RoboRepository.TryGetRoboId(session.CharacterId, request.ObjId, out var roboId)
        )
            ownsTarget = await roboRepository.ExistsAsync(
                checked((int)session.CharacterId),
                roboId,
                ct
            );

        var response = new EmotionCharaResponse(request.ObjId, ownsTarget ? 0u : 1u);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
        if (!ownsTarget)
            return;

        var notify = new NotifyEmotionChara(request.ObjId, request.EmotionId);
        byte[] data = notify.ToBytes();

        foreach (var other in state.GetAreaPeers(session, includeSelf: true))
        {
            await other.SendAsync(PacketType.NotifyEmotionChara, data, ct);
        }
    }
}
