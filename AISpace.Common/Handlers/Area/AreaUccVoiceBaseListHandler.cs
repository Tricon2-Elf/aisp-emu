using AISpace.Network.Packets.Area;
using AISpace.Network;

using AISpace.Common.Game;

namespace AISpace.Common.Handlers.Area;

public class AreaUccVoiceBaseListHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.UccVoiceBaseListRequest;

    public PacketType ResponseType => PacketType.UccVoiceBaseListResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new UccVoiceBaseListResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
