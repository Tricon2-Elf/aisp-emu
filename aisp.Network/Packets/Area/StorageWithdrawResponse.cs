using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_storage_withdraw_r (0xE42A). 12 bytes: UInt32 result, UInt64 success_aipoint (new deposit balance).
/// </summary>
public sealed class StorageWithdrawResponse(uint result, ulong successAiPoint) : IOutgoingPacket
{
    public uint Result { get; } = result;
    public ulong SuccessAiPoint { get; } = successAiPoint;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(SuccessAiPoint);
        return writer.ToBytes();
    }
}
