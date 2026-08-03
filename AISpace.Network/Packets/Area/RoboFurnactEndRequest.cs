namespace AISpace.Network.Packets.Area;

/// <summary>
/// send_robo_furnact_end (0xE7BC). Client→server; no direct response.
/// Payload: UInt roboid.
/// </summary>
public sealed class RoboFurnactEndRequest : IIncomingPacket<RoboFurnactEndRequest>
{
    public const int WireSize = 4;

    public uint RoboId { get; init; }

    public static RoboFurnactEndRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(RoboFurnactEndRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        var reader = new PacketReader(data);
        return new RoboFurnactEndRequest { RoboId = reader.ReadUInt() };
    }
}
