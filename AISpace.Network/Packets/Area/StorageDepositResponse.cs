using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// recv_storage_deposit_r (0x541C). 12 bytes: UInt32 result, UInt64 success_aipoint (new deposit balance).
/// </summary>
public sealed class StorageDepositResponse(uint result, ulong successAiPoint) : IOutgoingPacket
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
