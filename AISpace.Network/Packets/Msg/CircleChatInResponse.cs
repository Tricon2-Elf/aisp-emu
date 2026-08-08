using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleChatInResponse(uint result, uint changeFlag, IReadOnlyList<uint> avatarIds)
    : IOutgoingPacket
{
    public CircleChatInResponse(uint result)
        : this(result, 0, []) { }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(changeFlag);
        var count = Math.Min(avatarIds.Count, 100);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            writer.Write(avatarIds[i]);
        return writer.ToBytes();
    }
}
