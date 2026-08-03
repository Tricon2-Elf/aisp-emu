using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// send_robo_furnact_start (0x08F2). Client→server; no direct response.
/// Payload: UInt roboid, UInt furnid, MovementData start (14 bytes).
/// </summary>
public sealed class RoboFurnactStartRequest : IIncomingPacket<RoboFurnactStartRequest>
{
    public const int WireSize = 22;

    public uint RoboId { get; init; }
    public uint FurnitureId { get; init; }
    public MovementData Start { get; init; } = new(0, 0, 0, 0, MovementType.Stopped);

    public static RoboFurnactStartRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(RoboFurnactStartRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        var reader = new PacketReader(data);
        return new RoboFurnactStartRequest
        {
            RoboId = reader.ReadUInt(),
            FurnitureId = reader.ReadUInt(),
            Start = MovementData.FromBytes(reader.ReadBytes(14)),
        };
    }
}
