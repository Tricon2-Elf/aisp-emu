namespace AISpace.Network.Data;

public class ItemSlotInfo(uint id, uint socket)
{
    public uint ItemId = id; //4
    public uint Socket = socket; //4

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ItemId);
        writer.Write(Socket);
        return writer.ToBytes();
    }
}
