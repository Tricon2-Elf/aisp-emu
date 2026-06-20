namespace AISpace.Network.Data;

/// TPS (Third Person Shooter) battle parameters for a character — sub_798D80 in the client (175 bytes).
public class TpsBattleParams
{
    // sub_798B80 — char flags (18)
    public uint CharFlags0 { get; set; }
    public uint CharFlags4 { get; set; }
    public uint CharFlags8 { get; set; }
    public uint CharFlagsC { get; set; }
    public byte CharType { get; set; }      // byte_10 — char type/control flag
    public byte CharState { get; set; }     // byte_11 — char state flag

    // sub_798C00 — stamina/buffs (16)
    public float Stamina { get; set; }      // float_0
    public float Field14_Float4 { get; set; }
    public uint BuffParam1 { get; set; }    // dword_8 — CTPSBtlRepBuff type 21
    public uint BuffParam2 { get; set; }    // dword_c — CTPSBtlRepBuff type 22

    // sub_79A180 — HP/SP tank values (16)
    public uint TankCurrent { get; set; }   // dword_0
    public uint TankBaseMax { get; set; }   // dword_4
    public uint TankBonusMax { get; set; }  // dword_8 — max = base + bonus - reduction
    public uint TankReduction { get; set; } // dword_c

    // sub_798C60 — 4 skill arrays × 5 uint32 (80)
    public uint[] SkillArray0 { get; set; } = new uint[5]; // gap_0
    public uint[] SkillArray1 { get; set; } = new uint[5]; // dword_14
    public uint[] SkillArray2 { get; set; } = new uint[5]; // dword_28
    public uint[] SkillArray3 { get; set; } = new uint[5]; // dword_3c

    // sub_798D10 — int64 id + control bitmask (12)
    public ulong Field88Id { get; set; }     // dword_0, int64
    public uint ControlBitmask { get; set; } // dword_8 — bit 2 = special behavior

    // CSkillTable lookup key (4)
    public uint SkillTableId { get; set; }

    // sub_798D50 — cosplay/skill ref + cooldowns (29)
    public uint CosplaySkillRefId { get; set; } // dword_0 — cosplay table lookup key
    public byte SkillLevel { get; set; }         // field_8.byte_0
    public ulong Cooldown1 { get; set; }          // field_8.dword_8
    public ulong Cooldown2 { get; set; }          // field_8.dword_10
    public ulong Cooldown3 { get; set; }          // field_8.dword_18

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(CharFlags0);
        writer.Write(CharFlags4);
        writer.Write(CharFlags8);
        writer.Write(CharFlagsC);
        writer.Write(CharType);
        writer.Write(CharState);
        writer.Write(Stamina);
        writer.Write(Field14_Float4);
        writer.Write(BuffParam1);
        writer.Write(BuffParam2);
        writer.Write(TankCurrent);
        writer.Write(TankBaseMax);
        writer.Write(TankBonusMax);
        writer.Write(TankReduction);
        foreach (var v in SkillArray0) writer.Write(v);
        foreach (var v in SkillArray1) writer.Write(v);
        foreach (var v in SkillArray2) writer.Write(v);
        foreach (var v in SkillArray3) writer.Write(v);
        writer.Write(Field88Id);
        writer.Write(ControlBitmask);
        writer.Write(SkillTableId);
        writer.Write(CosplaySkillRefId);
        writer.Write(SkillLevel);
        writer.Write(Cooldown1);
        writer.Write(Cooldown2);
        writer.Write(Cooldown3);
        return writer.ToBytes(); // 175 bytes
    }
}
