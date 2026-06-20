using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaNpcGetDataHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.NpcGetDataRequest;

    public PacketType ResponseType => PacketType.NpcGetDataResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new NpcGetDataResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);

        if (session.MapId != StarterShopNpc.StarterMapId)
            return;

        var pos = new MovementData(StarterShopNpc.X, StarterShopNpc.Y, StarterShopNpc.Z, StarterShopNpc.Rotation, MovementType.Stopped);
        var npcChara = new CharaData(StarterShopNpc.ObjectId, StarterShopNpc.ModelId, StarterShopNpc.Name) { moveData = pos };
        npcChara.Visual.VisualId = StarterShopNpc.ObjectId;
        npcChara.AddEquip(
            DefaultClothingItems.Male.Select((itemId, slotIndex) => new CharacterEquipSlot((byte)slotIndex, (uint)itemId)),
            ItemEntityMapper.ResolveEquipSocket
        );

        var npcPacket = new NpcNotifyData(0, StarterShopNpc.ObjectId, npcChara).ToBytes();
        await session.SendAsync(PacketType.NpcNotifyData, npcPacket, ct);
    }
}
