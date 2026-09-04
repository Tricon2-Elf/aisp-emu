namespace aisp.Network.Packets.Area;

public sealed class FriendLinkTagChangeRequest(uint slot, string name)
    : IIncomingPacket<FriendLinkTagChangeRequest>
{
    public uint Slot { get; } = slot;
    public string Name { get; } = name;

    public static FriendLinkTagChangeRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new FriendLinkTagChangeRequest(reader.ReadUInt(), reader.ReadString());
    }
}
