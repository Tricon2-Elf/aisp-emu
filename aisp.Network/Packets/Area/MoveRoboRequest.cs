using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public sealed class MoveRoboRequest : IIncomingPacket<MoveRoboRequest>
{
    public uint RoboId { get; init; }
    public MovementData[] Moves { get; init; } = [];

    public static MoveRoboRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var roboId = reader.ReadUInt();
        var moves = new MovementData[2];
        for (var i = 0; i < 2; i++)
            moves[i] = MovementData.FromBytes(reader.ReadBytes(14));
        return new MoveRoboRequest { RoboId = roboId, Moves = moves };
    }
}
