namespace aisp.Network.Packets.Area;

/// <summary>Requests the cost of assigning a value to one of a Robo's five distributed status categories.</summary>
public sealed class DistributeStatusPointAddRequest
    : IIncomingPacket<DistributeStatusPointAddRequest>
{
    public uint RoboId { get; init; }
    public uint Type { get; init; }
    public uint Value { get; init; }

    public static DistributeStatusPointAddRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new DistributeStatusPointAddRequest
        {
            RoboId = reader.ReadUInt(),
            Type = reader.ReadUInt(),
            Value = reader.ReadUInt(),
        };
    }
}
