namespace aisp.Network.Data;

/// <summary>
/// Friend list identity consumed by the client's ReadFriendData routine.
/// The wire form is avatar ID followed by a null-terminated name of at most 36 bytes.
/// </summary>
public sealed record FriendData(uint AvatarId, string Name)
{
    public const int MaxFriends = 250;
    public const int NameLength = 37;

    public void Write(PacketWriter writer)
    {
        writer.Write(AvatarId);
        writer.Write(Name, NameLength - 1);
    }
}
