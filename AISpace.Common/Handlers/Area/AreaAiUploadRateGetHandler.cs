using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaAiUploadRateGetHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.AiUploadRateGetRequest;

    public PacketType ResponseType => PacketType.AiUploadRateGetResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new AiUploadRateGetResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
