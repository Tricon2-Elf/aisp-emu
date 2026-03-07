using AISpace.Common.Network.Packets.Area;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaAiDownloadListGetHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.AiDownloadListGetRequest;

    public PacketType ResponseType => PacketType.AiDownloadListGetResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var response = new AiDownloadListGetResponse();
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
