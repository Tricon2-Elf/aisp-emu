using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Game.ServerScripts;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaEventAccessNpcHandler(INpcRepository npcRepository, IShopRepository shopRepository, ServerScriptDispatcher serverScriptDispatcher, ILogger<AreaEventAccessNpcHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EventAccessNpcRequest;
    public PacketType ResponseType => PacketType.EventAccessNpcResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = EventAccessNpcRequest.FromBytes(payload.Span);

        logger.LogInformation("EventAccessNpcRequest from {CharacterId}: npc={NpcId}, pos=({X},{Y},{Z})", session.CharacterId, request.NpcId, request.AvatarX, request.AvatarY, request.AvatarZ);

        if (session.ActiveEventKey != null)
        {
            logger.LogWarning("Rejecting EventAccessNpcRequest for character {CharacterId}: already in event {EventKey}", session.CharacterId, session.ActiveEventKey);
            await session.SendAsync(ResponseType, new EventAccessNpcResponse(1).ToBytes(), ct);
            return;
        }

        var npc = await npcRepository.GetActiveByMapAndObjectIdAsync(session.MapId, session.ChannelId, request.NpcId, ct);
        if (npc is null || !npc.IsEnabled)
        {
            session.ActiveShopId = null;
            logger.LogWarning("Rejecting EventAccessNpcRequest for character {CharacterId}: map={MapId}, requestedNpc={NpcId}", session.CharacterId, session.MapId, request.NpcId);
            await session.SendAsync(ResponseType, new EventAccessNpcResponse(1).ToBytes(), ct);
            return;
        }

        if (npc.EventKind != NpcEventKind.None && !string.IsNullOrWhiteSpace(npc.EventKey))
        {
            session.ActiveShopId = null;
            switch (npc.EventKind)
            {
                case NpcEventKind.ClientScript:
                    await session.SendAsync(ResponseType, new EventAccessNpcResponse(0).ToBytes(), ct);
                    logger.LogInformation("Starting client script {EventKey} for character {CharacterId} via npc {NpcId}", npc.EventKey, session.CharacterId, request.NpcId);
                    await ClientScriptLauncher.StartAsync(session, npc.EventKey, EventCompletionPolicy.Replayable, ct);
                    return;
                case NpcEventKind.ServerScript:
                    if (!serverScriptDispatcher.HasScript(npc.EventKey))
                    {
                        logger.LogWarning("Rejecting EventAccessNpcRequest for character {CharacterId}: unknown server script {EventKey}", session.CharacterId, npc.EventKey);
                        await session.SendAsync(ResponseType, new EventAccessNpcResponse(1).ToBytes(), ct);
                        return;
                    }

                    var context = new ServerScriptContext { Npc = npc };
                    if (!await serverScriptDispatcher.CanStartAsync(session, npc.EventKey, context, ct))
                    {
                        logger.LogInformation("Rejecting EventAccessNpcRequest for character {CharacterId}: server script {EventKey} is not currently available", session.CharacterId, npc.EventKey);
                        await session.SendAsync(ResponseType, new EventAccessNpcResponse(1).ToBytes(), ct);
                        return;
                    }

                    await session.SendAsync(ResponseType, new EventAccessNpcResponse(0).ToBytes(), ct);
                    logger.LogInformation("Starting server script {EventKey} for character {CharacterId} via npc {NpcId}", npc.EventKey, session.CharacterId, request.NpcId);
                    await serverScriptDispatcher.StartAsync(session, npc.EventKey, context, serverScriptDispatcher.GetCompletionPolicy(npc.EventKey), ct);
                    return;
            }
        }

        if (npc.InteractionType != NpcInteractionType.Shop || npc.ShopId is null || npc.Shop is null || !npc.Shop.IsEnabled)
        {
            session.ActiveShopId = null;
            if (npc.InteractionType == NpcInteractionType.Decorative)
            {
                logger.LogDebug("Acknowledging decorative NPC access for character {CharacterId}: npc={NpcId}", session.CharacterId, request.NpcId);
                await session.SendAsync(ResponseType, new EventAccessNpcResponse(0).ToBytes(), ct);
                return;
            }

            logger.LogWarning("Rejecting EventAccessNpcRequest for character {CharacterId}: map={MapId}, requestedNpc={NpcId}", session.CharacterId, session.MapId, request.NpcId);
            await session.SendAsync(ResponseType, new EventAccessNpcResponse(1).ToBytes(), ct);
            return;
        }

        var shopItems = await shopRepository.GetEnabledItemsAsync(npc.ShopId.Value, ct);
        if (shopItems.Count == 0)
        {
            session.ActiveShopId = null;
            logger.LogWarning("Rejecting EventAccessNpcRequest for character {CharacterId}: npc={NpcId} has no enabled shop items", session.CharacterId, request.NpcId);
            await session.SendAsync(ResponseType, new EventAccessNpcResponse(1).ToBytes(), ct);
            return;
        }

        session.ActiveShopId = npc.ShopId.Value;
        var npcObjectId = checked((uint)npc.NpcObjectId);
        await session.SendAsync(ResponseType, new EventAccessNpcResponse(0).ToBytes(), ct);
        await session.SendAsync(PacketType.NotifySupplyNpcExec, new NotifySupplyNpcExec(npcObjectId).ToBytes(), ct);
        await session.SendAsync(PacketType.ShopStartedNotify, new ShopStartedNotify(npcObjectId, npc.Shop.DisplayName, checked((uint)npc.Shop.BannerVisualId)).ToBytes(), ct);
        await session.SendAsync(PacketType.ShopItemNotify, new ShopItemNotify(shopItems.Select(x => new ShopItemEntry((uint)x.ItemId, checked((ulong)x.AiPrice), checked((ulong)x.NicoPrice))).ToList()).ToBytes(), ct);
    }
}
