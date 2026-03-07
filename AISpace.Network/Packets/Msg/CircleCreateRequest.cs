using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleCreateRequest(string name, uint unk) : IPacket<CircleCreateRequest>
{
    public string Name = name;
    public uint Unk = unk; // Usually 0

    public static CircleCreateRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        // Based on log: 68-6A-00-00-00-00-00
        // "hj" + null (string) then 00 00 00 00 (uint)
        string name = reader.ReadString("Shift_JIS");
        uint unk = reader.ReadUInt();
        return new CircleCreateRequest(name, unk);
    }

    public byte[] ToBytes() => throw new NotImplementedException();
}
