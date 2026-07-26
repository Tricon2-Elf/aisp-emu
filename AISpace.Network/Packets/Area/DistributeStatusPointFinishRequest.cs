using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

/// <summary>Commits all five distributed status-point values for a Robo.</summary>
public sealed class DistributeStatusPointFinishRequest : IIncomingPacket<DistributeStatusPointFinishRequest>
{
    public uint RoboId { get; init; }
    public IReadOnlyList<uint> Values { get; init; } = [];

    public static DistributeStatusPointFinishRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var values = new uint[RoboData.DistributedStatusPointCount];
        var roboId = reader.ReadUInt();
        for (var index = 0; index < values.Length; index++)
            values[index] = reader.ReadUInt();

        return new DistributeStatusPointFinishRequest { RoboId = roboId, Values = values };
    }
}
