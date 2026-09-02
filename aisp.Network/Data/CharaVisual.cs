namespace aisp.Network.Data;


public class CharaVisual(
    BloodType bloodType,
    byte month,
    byte day,
    uint gender,
    uint visualId,
    byte face,
    uint hairStyle
)
{
    public BloodType BloodType = bloodType;
    public byte Month = month;
    public byte Day = day;
    public uint Gender = gender;
    public uint VisualId = visualId;
    public byte Face = face;
    public uint Hairstyle = hairStyle;
    public DateTime Birthdate => new(DateTime.Now.Year, Month, Day);

    public static CharaVisual FromBytes(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length != 19)
            throw new Exception("Invalid length");
        var reader = new PacketReader(buffer);

        var bloodType = reader.ReadUInt();
        var month = reader.ReadByte();
        var day = reader.ReadByte();
        var gender = reader.ReadUInt();
        var characterID = reader.ReadUInt();
        var face = reader.ReadByte();
        var hairstyle = reader.ReadUInt();
        var newCV = new CharaVisual(
            (BloodType)bloodType,
            month,
            day,
            gender,
            characterID,
            face,
            hairstyle
        );
        return newCV;
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)BloodType);
        writer.Write(Month);
        writer.Write(Day);
        writer.Write(Gender);
        writer.Write(VisualId);
        writer.Write(Face);
        writer.Write(Hairstyle);
        return writer.ToBytes();
    }

    public override string ToString()
    {
        return $"[CharaVisual] BloodType: {BloodType}, Month: {Month}, Day: {Day}, Gender: {Gender}, VisualId: {VisualId}, Face: {Face}, Hairstyle: {Hairstyle}";
    }
}
