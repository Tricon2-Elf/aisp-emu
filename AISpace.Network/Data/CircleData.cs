namespace AISpace.Network.Data;

public class CircleData(uint id, string name, uint leaderId)
{
    public uint Id = id;
    public string Name = name;
    public uint LeaderId = leaderId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();

        writer.Write(Id);
        writer.Write((uint)1); // Status

        writer.WriteFixedString(Name, 46);

        writer.Write(LeaderId);

        writer.Write(new byte[37]);

        writer.Write(new byte[20]);

        writer.Write(new byte[751]);
        return writer.ToBytes();
    }
}
