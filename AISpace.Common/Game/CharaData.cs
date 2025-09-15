namespace AISpace.Common.Game;

public class CharaData(uint chara_id, uint character_id, string name)
{

    public uint chara_id = chara_id;
    public uint character_id = character_id;
    public string name = name;
    public CharaVisual visual = new(1, 1, 1, 1, 2, 0, 0);
    public MoveData moveData = new(new System.Numerics.Vector3(0, 0, 0), 0, 0);
    //X-4069.790 Y-0.043 Z-2813.927
    public List<Game.ItemSlotInfo> Equips = new(30);

    public void AddEquip(uint id, uint socket)
    {
        Equips.Add(new ItemSlotInfo(id, socket));
    }
    //X-14.713552 Y-3541.8713 Z3.062385E+21
    public byte[] ToBytes()
    {
        using var writer = new Network.PacketWriter();
        writer.Write(chara_id, character_id);
        writer.WriteFixedAsciiString(name, 37);
        writer.Write(visual.ToBytes());
        //writer.Write(new byte[6]);
        writer.Write(moveData.ToBytes());
        writer.Write(moveData.ToBytes());
        writer.Write(new byte[6]);
        writer.Write(new byte[8]);
        writer.Write((ushort)Equips.Count);
        foreach (var item in Equips)
            writer.Write(item.ToBytes());
        writer.Write(new byte[217]);
        return writer.ToBytes();
    }
}
