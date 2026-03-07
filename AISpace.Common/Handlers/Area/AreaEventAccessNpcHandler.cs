using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaEventAccessNpcHandler(ILogger<AreaEventAccessNpcHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.EventAccessNpcRequest;
    public PacketType ResponseType => PacketType.EventAccessNpcResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = EventAccessNpcRequest.FromBytes(payload.Span);

        logger.LogInformation("EventAccessNpcRequest from {CharacterId}: npc={NpcId}, pos=({X},{Y},{Z})", session.CharacterId, request.NpcId, request.AvatarX, request.AvatarY, request.AvatarZ);

        await session.SendAsync(ResponseType, new EventAccessNpcResponse(0).ToBytes(), ct);
        await session.SendAsync(PacketType.NotifySupplyNpcExec, new NotifySupplyNpcExec(request.NpcId).ToBytes(), ct);
    }
}
