using AISpace.Network.Data;
namespace AISpace.Network.Packets.Msg;

public class AvatarDataResponse(uint avatarId, string name, uint modelId, uint islandId, uint slotId) : IPacket<AvatarDataResponse>
{
    public CharaVisual Visual = new(BloodType.A, 1, 1, 1, 2, 0, 0);
    public List<ItemSlotInfo> Equips = new(30);

    //equips?

    public static AvatarDataResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public void AddEquip(uint id, uint socket)
    {
        Equips.Add(new ItemSlotInfo(id, socket));
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(avatarId); // AvatarId
        writer.Write(name);
        writer.Write(modelId);
        writer.Write(Visual.ToBytes());
        writer.Write(islandId);
        writer.Write(slotId);
        foreach (var equip in Equips)
            writer.Write(equip.ToBytes());
        return writer.ToBytes();
    }
}
