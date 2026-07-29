namespace AISpace.Network.Packets.Area;

public sealed class BattleTargetLockRequest(uint targetObjectId)
    : IIncomingPacket<BattleTargetLockRequest>
{
    public uint TargetObjectId { get; } = targetObjectId;

    public static BattleTargetLockRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new BattleTargetLockRequest(reader.ReadUInt());
    }
}
