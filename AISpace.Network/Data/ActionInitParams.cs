namespace AISpace.Network.Data;

/// Action init params after the equipment array — dword_16c + float_170 Vec2.
/// Both consumed together in CCharaAction setup (sub_40A1D0):
/// dword_16c → CCharaAction::dword_c, float_170 → dword_8.
public class ActionInitParams
{
    public uint ActionParam { get; set; }    // dword_16c → CCharaAction::dword_c
    public float CameraFov { get; set; }     // float_170[0] — camera frustum FOV (sub_403BA0)
    public float ActionDir { get; set; }     // float_170[1] → CCharaAction::dword_8[1]

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ActionParam);
        writer.Write(CameraFov);
        writer.Write(ActionDir);
        return writer.ToBytes(); // 12 bytes
    }
}

