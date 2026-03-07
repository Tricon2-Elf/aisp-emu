namespace AISpace.Network.Packets.Auth;

public class WorldSelectRequest(uint SelectedID) : IPacket<WorldSelectRequest>
{
    public uint WorldID = SelectedID;

    public static WorldSelectRequest FromBytes(ReadOnlySpan<byte> data)
    {
        PacketReader reader = new(data);

        uint SelectedID = reader.ReadUInt();
        return new WorldSelectRequest(SelectedID);
    }

    public byte[] ToBytes()
    {
        throw new NotImplementedException();
    }
}
