using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class GetChannelListMapRequest : IIncomingPacket<GetChannelListMapRequest>
{
    public uint MapId { get; init; }

    public static GetChannelListMapRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new GetChannelListMapRequest { MapId = reader.ReadUInt() };
    }
}
