using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// recv_get_adventure_work_list_r contains a result, sheet number, work count,
/// and zero or more 16-byte work records. This response represents an empty list.
/// </summary>
public sealed class GetAdventureWorkListResponse(uint result = 0, uint sheetNumber = 0) : IOutgoingPacket
{
    public uint Result { get; } = result;
    public uint SheetNumber { get; } = sheetNumber;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(SheetNumber);
        writer.Write(0u);
        return writer.ToBytes();
    }
}
