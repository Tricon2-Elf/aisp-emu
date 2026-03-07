using AISpace.Common.Network.Packets.Area;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaEventAccessNpcHandler(ILogger<AreaEventAccessNpcHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.EventAccessNpcRequest;
    public PacketType ResponseType => PacketType.EventAccessNpcResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var request = EventAccessNpcRequest.FromBytes(payload.Span);

        logger.LogInformation("EventAccessNpcRequest from {CharacterId}: npc={NpcId}, pos=({X},{Y},{Z})", connection.CharacterId, request.NpcId, request.AvatarX, request.AvatarY, request.AvatarZ);

        await connection.SendAsync(ResponseType, new EventAccessNpcResponse(0).ToBytes(), ct);
        await connection.SendAsync(PacketType.NotifySupplyNpcExec, new NotifySupplyNpcExec(request.NpcId).ToBytes(), ct);
    }
}
