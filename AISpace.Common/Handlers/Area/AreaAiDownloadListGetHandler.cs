using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaAiDownloadListGetHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AiDownloadListGetRequest;

    public PacketType ResponseType => PacketType.AiDownloadListGetResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new AiDownloadListGetResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
