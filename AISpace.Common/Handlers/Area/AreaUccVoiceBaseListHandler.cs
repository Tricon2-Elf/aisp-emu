using AISpace.Common.Network.Packets.Area;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaUccVoiceBaseListHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.UccVoiceBaseListRequest;

    public PacketType ResponseType => PacketType.UccVoiceBaseListResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var response = new UccVoiceBaseListResponse();
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
