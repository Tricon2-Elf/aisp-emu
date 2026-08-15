using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaNpcGetDataHandler(INpcRepository npcRepository, ITextLocaliser localiser)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.NpcGetDataRequest;

    public PacketType ResponseType => PacketType.NpcGetDataResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var response = new NpcGetDataResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);

        var npcs = await npcRepository.GetActiveByMapAsync(session.MapId, session.ChannelId, ct);
        foreach (var npc in npcs)
            await SendNpcAsync(npc, session, localiser, ct);
    }

    private static Task SendNpcAsync(
        Npc npc,
        IPlayerSession session,
        ITextLocaliser localiser,
        CancellationToken ct
    )
    {
        var objectId = checked((uint)npc.NpcObjectId);
        var modelId = checked((uint)npc.ModelId);
        var pos = new MovementData(npc.X, npc.Y, npc.Z, npc.Rotation, MovementType.Stopped);
        var name = localiser.Get(session, L.Npc.Name(npc.NpcObjectId));
        var npcChara = new CharaData(objectId, modelId, name) { Movement = pos };
        npcChara.Visual.VisualId = objectId;
        npcChara.AddEquip(
            npc.Equipment.OrderBy(x => x.SortOrder)
                .ThenBy(x => x.SlotIndex)
                .Select(x => new CharacterEquipSlot(
                    checked((byte)x.SlotIndex),
                    checked((uint)x.ItemId)
                )),
            ItemEntityMapper.ResolveEquipSocket
        );

        var npcPacket = new NpcNotifyData(0, objectId, npcChara).ToBytes();
        return session.SendAsync(PacketType.NpcNotifyData, npcPacket, ct);
    }
}
