using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

public class AvatarMove(MovementData[] Moves) : IPacket<AvatarMove>
{
    public MovementData[] Moves = Moves;

    public static AvatarMove FromBytes(ReadOnlySpan<byte> data)
    {
        var packetReader = new PacketReader(data);
        int count = data.Length / 14;
        if (count == 0)
            count = 1;

        var movement = new MovementData[count];
        for (int i = 0; i < count; i++)
        {
            movement[i] = MovementData.FromBytes(packetReader.ReadBytes(14));
        }
        return new AvatarMove(movement);
    }

    public byte[] ToBytes() => throw new NotImplementedException();
}
