namespace AISpace.Network.Packets.Area;

/// <summary>
/// recv_myroom_throwout_others_r (0xB05A). Server-pushed to guests ejected from a My Room
/// (e.g. after the owner tightens security). No matching client send in the retail client.
/// Payload: UInt32 result (0 = success / ejected).
/// </summary>
public sealed class MyRoomThrowoutOthersResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
