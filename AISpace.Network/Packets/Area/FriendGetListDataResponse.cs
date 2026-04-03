using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class FriendGetListDataResponse : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); //Result
        writer.Write((uint)0); // friend_data
        writer.Write((uint)0); // already_in
        writer.Write((uint)0); // comment
        return writer.ToBytes();
    }
}
