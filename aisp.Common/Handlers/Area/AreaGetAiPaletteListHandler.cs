using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaGetAiPaletteListHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.GetAiPaletteListRequest;
    public PacketType ResponseType => PacketType.GetAiPaletteListResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = GetAiPaletteListRequest.FromBytes(payload.Span);
        await session.SendAsync(
            ResponseType,
            new GetAiPaletteListResponse(0, request.RoboId).ToBytes(),
            ct
        );
    }
}
