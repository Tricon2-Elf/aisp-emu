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
        writer.Write(Result);   // 4 байта
        writer.Write(AvatarId); // 4 байта
        
        // Координаты (12 байт: X, Y, Z)
        writer.Write(Move.X);
        writer.Write(Move.Y);
        writer.Write(Move.Z);
        
        // ВАЖНО: В коде были значения 1021/1051. 
        // Попробуем упаковать Rotation и Animation так, как ждет движок.
        writer.Write((sbyte)Move.Rotation); // 1 байт
        writer.Write((byte)Move.Animation); // 1 байт
        
        return writer.ToBytes(); // Итого 22 байта
    }
}