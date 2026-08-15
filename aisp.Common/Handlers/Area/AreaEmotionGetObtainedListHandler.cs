using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaEmotionGetObtainedListHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EmotionGetObtainedListRequest;
    public PacketType ResponseType => PacketType.EmotionGetObtainedListResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var ids = new List<uint>();

        // Unlock animations
        for (uint i = 1; i <= 36; i++)
            ids.Add(i);
        for (uint i = 100; i <= 105; i++)
            ids.Add(i);

        // Unlock base voices of the player
        for (uint i = 1; i <= 48; i++)
        {
            ids.Add(10101000 + i);
        }

        var response = new EmotionGetObtainedListResponse(0, ids);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
