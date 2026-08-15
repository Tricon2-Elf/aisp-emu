using aisp.Network.Data;

namespace aisp.Network.Packets.Auth;

public class WorldListResponse(uint Result, List<WorldData> Worlds) : IOutgoingPacket
{
    readonly int MaxNameLen = 97;
    readonly int MaxDescLen = 766;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write((uint)Worlds.Count);
        foreach (var world in Worlds)
        {
            writer.Write((uint)world.Id);
            writer.WriteFixedAsciiString(world.Name, MaxNameLen);
            writer.WriteFixedAsciiString(world.Description, MaxDescLen);
            writer.Write((uint)0); // WorldInfo.dword_364
        }
        return writer.ToBytes();
    }
}
