using System.Text;
using AISpace.Common.Network;

namespace AISpace.Common.Game;

public class CircleData(uint id, string name, uint leaderId)
{
    public uint Id = id;
    public string Name = name;
    public uint LeaderId = leaderId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        var enc = Encoding.GetEncoding("Shift_JIS");

        writer.Write(Id);
        writer.Write((uint)1); // Status

        byte[] nameBytes = enc.GetBytes(Name);
        byte[] finalName = new byte[46];
        Array.Copy(nameBytes, finalName, Math.Min(nameBytes.Length, 45));
        writer.Write(finalName);

        writer.Write(LeaderId);

        writer.Write(new byte[37]);

        writer.Write(new byte[20]);

        writer.Write(new byte[751]);
        return writer.ToBytes();
    }
}
