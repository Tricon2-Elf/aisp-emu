using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class AvatarSelectRequest(uint slotId) : IIncomingPacket<AvatarSelectRequest>
{
    public uint SlotId = slotId;

    public static AvatarSelectRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        uint slotId = reader.ReadUInt();
        return new AvatarSelectRequest(slotId);
    }
}
