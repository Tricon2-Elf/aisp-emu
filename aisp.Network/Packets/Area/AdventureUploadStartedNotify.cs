using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_adventure_upload_started (0x90BD): pushed by the server after the player talks to the drama disc shop's 買取担当
/// clerk. The client looks up the NPC object by the first field (its name and position go on the window) and opens
/// the drama upload window, then requests get_adventure_work_list and get_adventure_upload_list.
/// </summary>
public sealed class AdventureUploadStartedNotify(uint npcObjectId, uint value) : IOutgoingPacket
{
    public uint NpcObjectId { get; } = npcObjectId;
    public uint Value { get; } = value;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(NpcObjectId);
        writer.Write(Value);
        return writer.ToBytes();
    }
}
