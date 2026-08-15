namespace aisp.Network.Packets.Area;

/// <summary>recv_myroom_update_security_r (0xCE31): four-byte result code.</summary>
public sealed class MyRoomUpdateSecurityResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
