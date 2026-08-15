using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaAiDownloadListGetHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AiDownloadListGetRequest;

    public PacketType ResponseType => PacketType.AiDownloadListGetResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var response = new AiDownloadListGetResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
