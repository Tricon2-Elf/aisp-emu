namespace AISpace.Network.Packets.Area;

internal static class MyRoomFurniturePacketEncoding
{
    public static byte[] WriteResult(uint result)
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }

    public static byte[] WritePair(uint first, uint second)
    {
        var writer = new PacketWriter();
        writer.Write(first);
        writer.Write(second);
        return writer.ToBytes();
    }
}
