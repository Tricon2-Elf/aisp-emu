using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaGetRoboCreateInfoHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    private static readonly ItemSlotInfo[] DefaultEquips =
    [
        new(10100060, 0), // Shirt
        new(10200090, 0), // Shorts
        new(10400000, 0), // Socks
        new(10500010, 0), // Shoes
    ];

    private const uint DefaultModelId = 1002011;
    private const uint DefaultHairstyle = 10930010;

    public PacketType RequestType => PacketType.GetRoboCreateInfoRequest;
    public PacketType ResponseType => PacketType.GetRoboCreateInfoResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        _ = GetRoboCreateInfoRequest.FromBytes(payload.Span);
        var response = new GetRoboCreateInfoResponse(DefaultModelId, DefaultHairstyle, DefaultEquips);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
