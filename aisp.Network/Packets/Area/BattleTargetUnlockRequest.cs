namespace aisp.Network.Packets.Area;

public sealed class BattleTargetUnlockRequest(uint targetObjectId)
    : IIncomingPacket<BattleTargetUnlockRequest>
{
    public uint TargetObjectId { get; } = targetObjectId;

    public static BattleTargetUnlockRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4)
        {
            return new BattleTargetUnlockRequest(0);
        }
        var reader = new PacketReader(data);
        return new BattleTargetUnlockRequest(reader.ReadUInt());
    }
}
