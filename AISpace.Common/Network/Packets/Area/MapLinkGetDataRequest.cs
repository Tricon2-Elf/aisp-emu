namespace AISpace.Common.Network.Packets.Area;

public class MapLinkGetDataRequest : IPacket<MapLinkGetDataRequest>
{
    public uint MapId { get; set; }
    public uint ChannelId { get; set; }

    public static MapLinkGetDataRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new MapLinkGetDataRequest { MapId = reader.ReadUInt(), ChannelId = reader.ReadUInt() };
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(MapId);
        writer.Write(ChannelId);
        return writer.ToBytes();
    }
}
