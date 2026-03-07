using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaEmotionGetObtainedListHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.EmotionGetObtainedListRequest;
    public PacketType ResponseType => PacketType.EmotionGetObtainedListResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
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
