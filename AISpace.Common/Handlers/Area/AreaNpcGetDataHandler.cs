using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaNpcGetDataHandler(INpcRepository npcRepository)
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
            await SendNpcAsync(npc, session, ct);
    }

    private static Task SendNpcAsync(Npc npc, IPlayerSession session, CancellationToken ct)
    {
        var objectId = checked((uint)npc.NpcObjectId);
        var modelId = checked((uint)npc.ModelId);
        var pos = new MovementData(npc.X, npc.Y, npc.Z, npc.Rotation, MovementType.Stopped);
        var npcChara = new CharaData(objectId, modelId, npc.Name) { Movement = pos };
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
