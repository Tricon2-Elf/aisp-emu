using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public sealed class RoboCreateRequest : IIncomingPacket<RoboCreateRequest>
{
    public string Name { get; init; } = string.Empty;
    public CharaVisual Visual { get; init; } = new(0, 0, 0, 0, 0, 0, 0);
    public uint ModelId { get; init; }

    public static RoboCreateRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new RoboCreateRequest
        {
            Name = reader.ReadString("utf-8"),
            Visual = CharaVisual.FromBytes(reader.ReadBytes(19)),
            ModelId = reader.ReadUInt(),
        };
    }
}
