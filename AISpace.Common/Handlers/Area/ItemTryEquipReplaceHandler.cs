using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class ItemTryEquipReplaceHandler(ICharacterRepository characterRepo, IRoboRepository roboRepository, SharedState state, ILogger<ItemTryEquipReplaceHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ItemTryEquipReplaceRequest;
    public PacketType ResponseType => PacketType.ItemTryEquipReplaceResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = ItemTryEquipReplaceRequest.FromBytes(payload.Span);
        logger.LogInformation("Client {ConnectionId} ItemTryEquipReplace objId={ObjId} equipCount={Count}", session.ConnectionId, request.ObjId, request.Equips.Count);

        if (session.CharacterId == 0)
        {
            await session.SendAsync(ResponseType, new ItemTryEquipReplaceResponse(1).ToBytes(), ct);
            return;
        }

        var characterId = checked((int)session.CharacterId);
        var character = await characterRepo.GetByIdAsync(characterId, ct);
        var resolvedEquips = ResolveEquipsForPersistence(request.Equips, character);
        var normalizedEquips = NormalizeEquips(resolvedEquips);

        if (request.ObjId == session.CharacterId)
        {
            var replaceResult = await characterRepo.ReplaceEquipmentAsync(characterId, resolvedEquips, ct);
            session.Character = await characterRepo.GetByIdAsync(characterId, ct);

            await SendReplaceSuccessAsync(session, request.ObjId, normalizedEquips, ct);
            await CharacterItemSync.SendReplaceChangesAsync(session, replaceResult, ct);

            if (session.Character is not null)
            {
                var appearanceNotify = BuildAppearanceNotify(session, session.Character);
                foreach (var peer in state.GetAreaPeers(session))
                    await peer.SendAsync(PacketType.AvatarNotifyData, appearanceNotify, ct);
            }

            return;
        }

        if (!RoboRepository.TryGetRoboId(session.CharacterId, request.ObjId, out var roboId))
        {
            await session.SendAsync(ResponseType, new ItemTryEquipReplaceResponse(1).ToBytes(), ct);
            return;
        }

        var robo = await roboRepository.ReplaceEquipmentAsync(characterId, roboId, resolvedEquips, ct);
        if (robo is null)
        {
            await session.SendAsync(ResponseType, new ItemTryEquipReplaceResponse(1).ToBytes(), ct);
            return;
        }

        await SendReplaceSuccessAsync(session, request.ObjId, normalizedEquips, ct);

        var update = new NotifyUpdateRoboEquip(roboId, request.ObjId, TryEquipNotifyBuilder.FromRobo(robo)).ToBytes();
        foreach (var peer in state.GetAreaPeers(session, includeSelf: true))
            await peer.SendAsync(PacketType.NotifyUpdateRoboEquip, update, ct);
    }

    private static List<ItemEquipEntry> NormalizeEquips(IReadOnlyList<ItemEquipEntry> equips)
    {
        return equips
            .Select(e =>
            {
                var socket = ItemEntityMapper.ResolveBodyspot((int)e.ItemId);
                if (socket == 0)
                    socket = e.SocketBit;
                return new ItemEquipEntry(e.ItemId, socket);
            })
            .ToList();
    }

    private static async Task SendReplaceSuccessAsync(IPlayerSession session, uint objectId, IReadOnlyList<ItemEquipEntry> equipment, CancellationToken ct)
    {
        await session.SendAsync(PacketType.ItemTryEquipReplaceResponse, new ItemTryEquipReplaceResponse(0).ToBytes(), ct);
        await session.SendAsync(PacketType.ItemTryEquipReplacedNotify, new ItemTryEquipReplacedNotify(objectId, equipment).ToBytes(), ct);
    }

    private static IReadOnlyList<ItemEquipEntry> ResolveEquipsForPersistence(IReadOnlyList<ItemEquipEntry> equips, Character? character)
    {
        if (character is null || equips.Count == 0)
            return equips;

        var ownedItemIds = character.Inventory.Select(x => x.ItemId).Concat(character.Equipment.Select(x => x.ItemId)).Where(x => x > 0).Distinct().ToList();
        if (ownedItemIds.Count == 0)
            return equips;

        return equips
            .Select(e =>
            {
                var resolved = ResolveRequestItemId(e.ItemId, e.SocketBit, ownedItemIds);
                return resolved == e.ItemId ? e : new ItemEquipEntry(resolved, e.SocketBit);
            })
            .ToList();
    }

    private static uint ResolveRequestItemId(uint requestItemId, uint socketBit, IReadOnlyList<int> ownedItemIds)
    {
        // Client try-equip packets can carry either full item id or serial id.
        // Accept both and map serial ids back to owned item ids deterministically.
        if (ownedItemIds.Contains((int)requestItemId))
            return requestItemId;

        var candidates = ownedItemIds.Where(id => ResolveLegacySerialId(id) == requestItemId).ToList();
        if (candidates.Count == 0)
            return requestItemId;

        if (candidates.Count == 1)
            return (uint)candidates[0];

        // Disambiguate serial collisions using requested socket/bodyspot when possible.
        var bySocket = candidates.Where(id => ItemEntityMapper.ResolveBodyspot(id) == socketBit).ToList();
        if (bySocket.Count == 1)
            return (uint)bySocket[0];

        return (uint)candidates[0];
    }

    private static uint ResolveLegacySerialId(int itemId)
    {
        if (itemId < 100_000)
            return (uint)itemId + 1;

        if (itemId is >= 10_000_000 and < 200_000_000)
            return (uint)(itemId % 100_000 + 1);

        if (itemId is >= 200_000_000 and < 300_000_000)
            return (uint)((itemId / 1_000) % 100_000 + 1);

        return unchecked((uint)itemId + 1);
    }

    private static byte[] BuildAppearanceNotify(IPlayerSession session, Character character)
    {
        var pos = new MovementData(session.X, session.Y, session.Z, session.Rotation, MovementType.Stopped);
        return AreasvEnterHandler.CreateNotify(character, session.CharacterId, 1, pos);
    }
}
