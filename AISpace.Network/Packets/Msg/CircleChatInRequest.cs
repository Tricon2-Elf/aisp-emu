using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleChatInRequest : IPacket<CircleChatInRequest>
{
    public uint CircleId;
    public uint Unk;

    public static CircleChatInRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new CircleChatInRequest { CircleId = reader.ReadUInt(), Unk = reader.ReadUInt() };
    }

    public byte[] ToBytes() => throw new NotImplementedException();
}
