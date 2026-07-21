using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public sealed class MonsterGetDataRequest(uint mapId, uint channelId) : IIncomingPacket<MonsterGetDataRequest>
{
    public uint MapId { get; } = mapId;
    public uint ChannelId { get; } = channelId;

    public static MonsterGetDataRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new MonsterGetDataRequest(reader.ReadUInt(), reader.ReadUInt());
    }
}
