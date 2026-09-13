using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_niconi_commons_shop_started (0x7D98). Switch at 0x7D801A (opcode 0x7D78+0x20) enters
/// case 0x12F at 0x7D8205. Parser: u32, C-string via 0x796BB0 (max 0xC1 including NUL), u32.
/// Remaining-bytes must be empty or vtable+0x60 drops Area.
///
/// Success calls CProtoArea_client vtable+0x36C (0x78F040) → 0x4CC720, which looks up IF
/// window 0x9D (CCommonsShopWindow@IF) and shows it. Clothing recv_shop_started is the same
/// three fields plus a talks u32; commons synthesizes talks=0 inside 0x4CC720.
///
/// Opcode 0x7D78 is recv_notify_avatar_data (vtable+0xB0 / 0x78C670, npc+AvatarData 932).
/// That path handles clerk appearance without opening the shop window. It copies the
/// clerk into tps::CTPSActionMgr when IsOpen, or causes an access violation at 0x40E400.
/// </summary>
public sealed class NiconiCommonsShopStartedNotify(uint npcObjectId, string name, uint visualId = 0)
    : IOutgoingPacket
{
    public const int NameMaxBytes = 192;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(npcObjectId);
        writer.Write(name, NameMaxBytes);
        writer.Write(visualId);
        return writer.ToBytes();
    }
}
