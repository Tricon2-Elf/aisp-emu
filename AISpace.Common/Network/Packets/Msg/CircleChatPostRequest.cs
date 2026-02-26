namespace AISpace.Common.Network.Packets.Msg;

public class CircleChatPostRequest : IPacket<CircleChatPostRequest>
{
    public uint CircleId;
    public string Message = string.Empty;
    public uint BalloonId;

    public static CircleChatPostRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new CircleChatPostRequest
        {
            CircleId = reader.ReadUInt(),
            Message = reader.ReadString("Shift_JIS"),
            BalloonId = reader.ReadUInt(),
        };
    }

    public byte[] ToBytes() => throw new NotImplementedException();
}
