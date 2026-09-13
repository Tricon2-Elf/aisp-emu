namespace aisp.Network.Data;

/// <summary>0x79B350: two u32s, name[96], owned byte, description[765], trailing u32.</summary>
public sealed record UccVoice(uint Id, uint IconId, string Name, bool Owned, string Description)
{
    public const int WireSize = 874;

    public void Write(PacketWriter writer)
    {
        writer.Write(Id);
        writer.Write(IconId);
        writer.WriteFixedStringNulTerminated(Name, 96);
        writer.Write(Owned ? (byte)1 : (byte)0);
        writer.WriteFixedStringNulTerminated(Description, 765);
        writer.Write(0u);
    }
}
