using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaGetAiPaletteListHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.GetAiPaletteListRequest;
    public PacketType ResponseType => PacketType.GetAiPaletteListResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = GetAiPaletteListRequest.FromBytes(payload.Span);
        await session.SendAsync(ResponseType, new GetAiPaletteListResponse(0, request.RoboId).ToBytes(), ct);
    }
}
