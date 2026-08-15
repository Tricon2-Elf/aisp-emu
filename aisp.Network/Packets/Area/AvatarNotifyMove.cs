using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public class AvatarNotifyMove(uint avatarId, IReadOnlyList<MovementData> moves) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)moves.Count);
        foreach (var move in moves)
        {
            writer.Write(avatarId);
            writer.Write(move.ToBytes());
        }

        return writer.ToBytes();
    }
}
