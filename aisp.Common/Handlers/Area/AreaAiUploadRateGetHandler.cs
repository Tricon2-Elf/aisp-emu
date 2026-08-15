using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaAiUploadRateGetHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AiUploadRateGetRequest;

    public PacketType ResponseType => PacketType.AiUploadRateGetResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var response = new AiUploadRateGetResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
