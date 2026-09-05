using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>recv_adventure_work_delete_r (0x2083), 6 bytes: UInt Result, UShort WorkId.</summary>
public sealed class AdventureWorkDeleteResponse(uint result, ushort workId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(workId);
        return writer.ToBytes();
    }
}
