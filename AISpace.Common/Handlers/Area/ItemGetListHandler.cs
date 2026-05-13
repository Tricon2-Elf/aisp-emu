using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class ItemGetListHandler(ILogger<ItemGetListHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ItemGetListRequest;
    public PacketType ResponseType => PacketType.ItemGetListResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        logger.LogInformation("Client {Id} requested ItemGetList", session.ConnectionId);

        var response = new ItemGetListResponse(0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
