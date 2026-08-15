using aisp.Network;

namespace aisp.Network.Packets.Area;

public class MapLinkGetDataRequest : IIncomingPacket<MapLinkGetDataRequest>
{
    public uint MapId { get; set; }
    public uint ChannelId { get; set; }

    public static MapLinkGetDataRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new MapLinkGetDataRequest
        {
            MapId = reader.ReadUInt(),
            ChannelId = reader.ReadUInt(),
        };
    }
}
