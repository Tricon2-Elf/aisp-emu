namespace aisp.Network.Packets.Auth;

using aisp.Network;

public class WorldSelectRequest(uint SelectedID) : IIncomingPacket<WorldSelectRequest>
{
    public uint WorldID = SelectedID;

    public static WorldSelectRequest FromBytes(ReadOnlySpan<byte> data)
    {
        PacketReader reader = new(data);

        uint SelectedID = reader.ReadUInt();
        return new WorldSelectRequest(SelectedID);
    }
}
