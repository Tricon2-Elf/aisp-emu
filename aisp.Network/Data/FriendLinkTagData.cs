namespace aisp.Network.Data;

/// <summary>Client tag record: a 32-bit identifier followed by a fixed 61-byte label.</summary>
public sealed record FriendLinkTagData(uint Id, string Name)
{
    // ReadTagData (client 0x799150) consumes 0x3D bytes. The resulting
    // 65-byte wire record is padded to 68 bytes only in client memory.
    public const int NameBytes = 0x3D;

    public void Write(PacketWriter writer)
    {
        writer.Write(Id);
        writer.WriteFixedString(Name, NameBytes);
    }
}
