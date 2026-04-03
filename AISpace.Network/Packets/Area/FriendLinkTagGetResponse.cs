namespace AISpace.Network.Packets.Area;

public class FriendLinkTagGetResponse : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        PacketWriter writer = new();
        writer.Write((uint)0); //Result
        writer.Write((uint)0); //avatar_id
        writer.Write((uint)0); // tagdata
        writer.Write((uint)0); // slot
        writer.Write((uint)0); // questionnaire_tagdata
        writer.Write((uint)0); // questionnaire_slot
        return writer.ToBytes();
    }
}
