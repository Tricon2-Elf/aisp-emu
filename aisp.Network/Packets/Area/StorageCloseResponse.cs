using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_storage_close_r (0x3D14). 4 bytes: UInt32 result (0 = success).
/// </summary>
public sealed class StorageCloseResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
