using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class FriendLinkTagGetResponse(
    uint result,
    uint avatarId,
    uint tagData = 0,
    uint slot = 0,
    uint questionnaireTagData = 0,
    uint questionnaireSlot = 0
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        PacketWriter writer = new();
        writer.Write(result);
        writer.Write(avatarId);
        writer.Write(tagData);
        writer.Write(slot);
        writer.Write(questionnaireTagData);
        writer.Write(questionnaireSlot);
        return writer.ToBytes();
    }
}
