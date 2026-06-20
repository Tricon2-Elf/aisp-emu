namespace AISpace.Network.Data;

/// Character state flags and timestamps — sub_798B10 in the client (25 bytes).
public class CharacterStateParams
{
    public byte StateFlag { get; set; }       // byte_0 — character state flag
    public ulong PermissionMask { get; set; } // dword_8, int64 — capability/permission bitmask (bit 1, 4 checked)
    public ulong Timestamp1 { get; set; }     // dword_10, int64 — cooldown/duration timestamp
    public ulong Timestamp2 { get; set; }     // dword_18, int64 — cooldown/duration timestamp

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(StateFlag);
        writer.Write(PermissionMask);
        writer.Write(Timestamp1);
        writer.Write(Timestamp2);
        return writer.ToBytes(); // 25 bytes
    }
}
