using AISpace.Common.Game;
using Microsoft.EntityFrameworkCore.Storage.Json;

namespace AISpace.Common.Network.Packets.Avatar;

public class AvatarNotifyMove(uint avatar_Id, MoveData moveData) : IPacket<AvatarNotifyMove>
{
    public static AvatarNotifyMove FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)1);
        writer.Write((uint)avatar_Id);
        writer.Write(moveData.ToBytes());
        return writer.ToBytes();
    }
}
