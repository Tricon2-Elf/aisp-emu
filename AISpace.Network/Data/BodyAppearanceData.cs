namespace AISpace.Network.Data;

/// Body appearance customization data — cls_7991A0 in the client (53 bytes).
/// Applied via character controller vfunc_308. Used in chr-fullset-make and doll creation.
public class BodyAppearanceData
{
    public byte[] BodyParams { get; set; } = new byte[49]; // gap_0[49] — proportions, colors, scale
    public uint Trailer { get; set; }                       // dword_34 — version/reserved marker

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(BodyParams);
        writer.Write(Trailer);
        return writer.ToBytes(); // 53 bytes
    }
}
