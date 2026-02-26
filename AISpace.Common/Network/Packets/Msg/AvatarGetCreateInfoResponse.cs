namespace AISpace.Common.Network.Packets.Msg;

public class AvatarGetCreateInfoResponse : IPacket<AvatarGetCreateInfoResponse>
{
    // исправил мужской рост
    private readonly List<uint> DefaultMaleBuilds = [1001021, 1001011, 1001031];
    private readonly List<byte> DefaultMaleFaces = [0, 1, 2, 3];
    private readonly List<uint> DefaultMaleHairStyles = [10920010, 10920020, 10920040];
    private readonly List<byte> DefaultMaleHairColours = [0, 4, 1, 2, 3];
    private readonly List<Game.ItemSlotInfo> DefaultMaleEquipment = [];

    // женский тоже исправил
    private readonly List<uint> DefaultFemaleBuilds = [1002011, 1002021, 1002031];
    private readonly List<byte> DefaultFemaleFaces = [0, 1, 2, 3];
    private readonly List<uint> DefaultFemaleHairStyles = [10930010, 10930020, 10930040];
    private readonly List<byte> DefaultFemaleHairColours = [0, 4, 1, 2, 3];
    private readonly List<Game.ItemSlotInfo> DefaultFemaleEquipment = [];

    public static AvatarGetCreateInfoResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();

    public byte[] ToBytes()
    {
        DefaultMaleEquipment.Clear();
        DefaultMaleEquipment.Add(new Game.ItemSlotInfo(10100220, 0)); 
        DefaultMaleEquipment.Add(new Game.ItemSlotInfo(10200100, 0)); 
        DefaultMaleEquipment.Add(new Game.ItemSlotInfo(10400030, 0)); 
        DefaultMaleEquipment.Add(new Game.ItemSlotInfo(10500070, 0)); 

        DefaultFemaleEquipment.Clear();
        DefaultFemaleEquipment.Add(new Game.ItemSlotInfo(10100060, 0)); 
        DefaultFemaleEquipment.Add(new Game.ItemSlotInfo(10200090, 0)); 
        DefaultFemaleEquipment.Add(new Game.ItemSlotInfo(10400000, 0)); 
        DefaultFemaleEquipment.Add(new Game.ItemSlotInfo(10500010, 0)); 

        var writer = new PacketWriter();
        // Male
        writer.Write((uint)DefaultMaleBuilds.Count);
        foreach (var v in DefaultMaleBuilds) writer.Write(v);
        writer.Write((uint)DefaultMaleFaces.Count);
        foreach (var v in DefaultMaleFaces) writer.Write(v);
        writer.Write((uint)DefaultMaleHairStyles.Count);
        foreach (var v in DefaultMaleHairStyles) writer.Write(v);
        writer.Write((uint)DefaultMaleHairColours.Count);
        foreach (var v in DefaultMaleHairColours) writer.Write(v);
        writer.Write((uint)DefaultMaleEquipment.Count);
        foreach (var eq in DefaultMaleEquipment) writer.Write(eq.ToBytes());
        // Female
        writer.Write((uint)DefaultFemaleBuilds.Count);
        foreach (var v in DefaultFemaleBuilds) writer.Write(v);
        writer.Write((uint)DefaultFemaleFaces.Count);
        foreach (var v in DefaultFemaleFaces) writer.Write(v);
        writer.Write((uint)DefaultFemaleHairStyles.Count);
        foreach (var v in DefaultFemaleHairStyles) writer.Write(v);
        writer.Write((uint)DefaultFemaleHairColours.Count);
        foreach (var v in DefaultFemaleHairColours) writer.Write(v);
        writer.Write((uint)DefaultFemaleEquipment.Count);
        foreach (var eq in DefaultFemaleEquipment) writer.Write(eq.ToBytes());

        return writer.ToBytes();
    }
}