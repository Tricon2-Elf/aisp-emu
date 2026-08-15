using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public class AvatarNotifyData(uint Result, AvatarData avatarData) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(avatarData.ToBytes());
        return writer.ToBytes();
    }
}
