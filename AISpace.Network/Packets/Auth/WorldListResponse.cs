using AISpace.Network.Data;

namespace AISpace.Network.Packets.Auth;

public class WorldListResponse(uint Result, List<WorldData> Worlds) : IPacket<WorldListResponse>
{
    readonly int MaxNameLen = 97;
    readonly int MaxDescLen = 766;

    public static WorldListResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

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
