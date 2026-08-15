using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaUccVoiceBaseListHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.UccVoiceBaseListRequest;

    public PacketType ResponseType => PacketType.UccVoiceBaseListResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var response = new UccVoiceBaseListResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
