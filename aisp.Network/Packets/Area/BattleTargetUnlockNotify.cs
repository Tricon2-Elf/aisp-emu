namespace aisp.Network.Packets.Area;

public sealed class BattleTargetUnlockNotify(uint targetObjectId) : IOutgoingPacket
{
    public uint TargetObjectId { get; } = targetObjectId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(TargetObjectId);
        return writer.ToBytes();
    }
}
