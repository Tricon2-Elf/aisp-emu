using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaRoboVoiceTypeUpdateHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboVoiceTypeUpdateRequest;
    public PacketType ResponseType => PacketType.RoboVoiceTypeUpdateResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = RoboVoiceTypeUpdateRequest.FromBytes(payload.Span);
        var response = new RoboVoiceTypeUpdateResponse(0, req.VoiceType);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
