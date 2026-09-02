namespace aisp.Network.Data;

/// <summary>
/// Friend list identity consumed by the client's ReadFriendData routine.
/// The wire form is avatar ID followed by a fixed 37-byte name.
/// </summary>
public sealed record FriendData(uint AvatarId, string Name)
{
    public const int MaxFriends = 250;
    public const int NameLength = 37;
    public const int WireSize = sizeof(uint) + NameLength;

    public void Write(PacketWriter writer)
    {
        writer.Write(AvatarId);
        writer.WriteFixedString(Name, NameLength);
    }
}
