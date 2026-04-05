using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class MapLinkGetDataResponse : IOutgoingPacket
{
    public uint Result { get; set; }

    public MapLinkGetDataResponse() { }

    public MapLinkGetDataResponse(uint result)
    {
        Result = result;
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
