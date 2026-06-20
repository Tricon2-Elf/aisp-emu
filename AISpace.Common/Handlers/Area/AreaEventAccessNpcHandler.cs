using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaEventAccessNpcHandler(ILogger<AreaEventAccessNpcHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EventAccessNpcRequest;
    public PacketType ResponseType => PacketType.EventAccessNpcResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = EventAccessNpcRequest.FromBytes(payload.Span);

        logger.LogInformation("EventAccessNpcRequest from {CharacterId}: npc={NpcId}, pos=({X},{Y},{Z})", session.CharacterId, request.NpcId, request.AvatarX, request.AvatarY, request.AvatarZ);

        if (session.MapId != StarterShopNpc.StarterMapId || request.NpcId != StarterShopNpc.ObjectId)
        {
            logger.LogWarning(
                "Rejecting EventAccessNpcRequest for character {CharacterId}: map={MapId}, requestedNpc={NpcId}, expectedNpc={ExpectedNpcId}",
                session.CharacterId,
                session.MapId,
                request.NpcId,
                StarterShopNpc.ObjectId
            );
            await session.SendAsync(ResponseType, new EventAccessNpcResponse(1).ToBytes(), ct);
            return;
        }

        await session.SendAsync(ResponseType, new EventAccessNpcResponse(0).ToBytes(), ct);
        await session.SendAsync(PacketType.NotifySupplyNpcExec, new NotifySupplyNpcExec(StarterShopNpc.ObjectId).ToBytes(), ct);
        await session.SendAsync(PacketType.ShopStartedNotify, new ShopStartedNotify(StarterShopNpc.ObjectId, StarterShopNpc.Name, StarterShopNpc.ObjectId).ToBytes(), ct);
        await session.SendAsync(
            PacketType.ShopItemNotify,
            new ShopItemNotify(StarterShopCatalog.Items.Select(x => new ShopItemNotify.ShopItem(x.ItemId, x.NpsPrice, x.NiconicoPrice)).ToList()).ToBytes(),
            ct
        );
    }
}
