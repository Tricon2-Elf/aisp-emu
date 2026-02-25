using AISpace.Common.Game;
using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class AvatarNotifyMove : IPacket<AvatarNotifyMove>
{
    public uint Result { get; set; }
    public uint AvatarId { get; set; }
    public MovementData Move { get; set; }

    public AvatarNotifyMove(uint result, uint avatarId, MovementData move)
    {
        Result = result;
        AvatarId = avatarId;
        Move = move;
    }

    public static AvatarNotifyMove FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        
        writer.Write(Result);
        writer.Write(AvatarId);
        
        writer.Write(Move.X);
        writer.Write(Move.Y);
        writer.Write(Move.Z);
        writer.Write((sbyte)Move.Rotation);
        writer.Write((byte)Move.Animation);
        
        return writer.ToBytes();
    }
}
