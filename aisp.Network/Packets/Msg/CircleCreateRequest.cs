using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class CircleCreateRequest(string name, uint markId) : IIncomingPacket<CircleCreateRequest>
{
    public string Name = name;
    public uint MarkId = markId;

    public static CircleCreateRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new CircleCreateRequest(reader.ReadString("utf-8"), reader.ReadUInt());
    }
}
