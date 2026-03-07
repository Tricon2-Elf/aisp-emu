using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleChatForwardNotify(uint circleId, uint fromAvatarId, string message, uint balloonId) : IPacket<CircleChatForwardNotify>
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(circleId);
        writer.Write(fromAvatarId);
        writer.Write(message, "Shift_JIS");
        writer.Write(balloonId);
        return writer.ToBytes();
    }

    public static CircleChatForwardNotify FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();
}
