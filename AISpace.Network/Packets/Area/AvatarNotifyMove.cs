using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

public class AvatarNotifyMove(uint Result, uint avatar_Id, MovementData moveData) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result); //Should be 1
        writer.Write(avatar_Id);
        writer.Write(moveData.ToBytes());
        return writer.ToBytes();
    }
}
