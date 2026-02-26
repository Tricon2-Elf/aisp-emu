namespace AISpace.Common.Network.Packets.Msg;

public class AvatarDataResponse(uint result, string name, uint modelId, uint islandId, uint slotId) : IPacket<AvatarDataResponse>
{
    public Game.CharaVisual Visual = new(Game.BloodType.A, 1, 1, 1, 2, 0, 0);
    public List<Game.ItemSlotInfo> Equips = new(30);

    //equips?

    public static AvatarDataResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public void AddEquip(uint id, uint socket)
    {
        Equips.Add(new Game.ItemSlotInfo(id, socket));
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        var _equips = Equips;
        while (_equips.Count < 30)
            AddEquip(0, 0);

        writer.Write(name);
        writer.Write(modelId);
        writer.Write(Visual.ToBytes());
        writer.Write(islandId);
        writer.Write(slotId);
        foreach (var equip in _equips)
            writer.Write(equip.ToBytes());
        return writer.ToBytes();
    }
}
