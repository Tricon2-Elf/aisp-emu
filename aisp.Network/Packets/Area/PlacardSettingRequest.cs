using System.Numerics;

namespace aisp.Network.Packets.Area;

/// <summary>The 21-byte request sent when placing a Friend Link placard.</summary>
public sealed class PlacardSettingRequest(uint type, uint slot, Vector3 position, byte direction)
    : IIncomingPacket<PlacardSettingRequest>
{
    public uint Type { get; } = type;
    public uint Slot { get; } = slot;
    public Vector3 Position { get; } = position;
    public byte Direction { get; } = direction;

    public static PlacardSettingRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new PlacardSettingRequest(
            reader.ReadUInt(),
            reader.ReadUInt(),
            new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat()),
            reader.ReadByte()
        );
    }
}
