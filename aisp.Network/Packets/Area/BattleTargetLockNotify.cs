namespace aisp.Network.Packets.Area;

public sealed class BattleTargetLockNotify(
    uint actionObjectId,
    uint targetObjectId,
    uint lockMode = 1
) : IOutgoingPacket
{
    public uint ActionObjectId { get; } = actionObjectId;
    public uint TargetObjectId { get; } = targetObjectId;
    public uint LockMode { get; } = lockMode;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ActionObjectId);
        writer.Write(TargetObjectId);
        writer.Write(LockMode);
        return writer.ToBytes();
    }
}
