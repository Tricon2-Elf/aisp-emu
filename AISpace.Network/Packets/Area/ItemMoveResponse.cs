using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>recv_item_move_r (0x708B). 4 bytes: UInt32 result (0 = success).</summary>
public sealed class ItemMoveResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
