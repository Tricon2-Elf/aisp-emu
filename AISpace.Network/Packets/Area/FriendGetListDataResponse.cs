using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class FriendGetListDataResponse : IPacket<FriendGetListDataResponse>
{
    public static FriendGetListDataResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

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
