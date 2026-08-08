namespace AISpace.Network.Packets.Area;

/// <summary>recv_edit_robo_myprofile_r (0x2180): result.</summary>
public sealed class EditRoboMyProfileResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
