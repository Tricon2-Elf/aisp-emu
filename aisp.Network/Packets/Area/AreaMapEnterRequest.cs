using aisp.Network;

namespace aisp.Network.Packets.Area;

public class AreaMapEnterRequest : IIncomingPacket<AreaMapEnterRequest>
{
    public uint MapID { get; set; }
    public uint ChannelId { get; set; }

    public static AreaMapEnterRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new AreaMapEnterRequest { MapID = reader.ReadUInt(), ChannelId = reader.ReadUInt() };
    }
}
