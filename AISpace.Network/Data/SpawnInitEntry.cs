namespace AISpace.Network.Data;

/// Single spawn initialization entry — sub_798440 in the client (37 bytes).
/// Controls one aspect of character spawn: texture load, AI config, gender, position, etc.
/// Up to 8 entries are sent per avatar.
public class SpawnInitEntry
{
    public uint EntryId { get; set; }       // unused in known client paths
    public uint SpawnFlags { get; set; }    // == 1 → CCharaController::duplicate_2_10
    public uint Reserved { get; set; }      // unused
    public uint ActionType { get; set; }    // 0/2/3=texture, 1=skill cast, 7=effect
    public uint TextureId { get; set; }     // texture index (types 0/2/3) or action/effect ID (types 1/7)
    public uint CtrlConfig { get; set; }    // CEnemyController::func_54 param
    public uint SpawnHeight { get; set; }   // float position offset → dxObject::func_12
    public uint Gender { get; set; }        // CCharaController::func_61 → m_Gender
    public uint CtrlState { get; set; }     // CAICharaParam::func_7 param

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(EntryId);
        writer.Write(SpawnFlags);
        writer.Write(Reserved);
        writer.Write(ActionType);
        writer.Write(TextureId);
        writer.Write(CtrlConfig);
        writer.Write(SpawnHeight);
        writer.Write(Gender);
        writer.Write(CtrlState);
        writer.Write((byte)0); // terminator
        return writer.ToBytes(); // 37 bytes
    }
}
