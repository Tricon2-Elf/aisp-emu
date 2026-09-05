namespace aisp.Network.Packets.Msg;

/// <summary>One fixed 422-byte placard comment record consumed by the retail client.</summary>
public sealed record PlacardCommentLogEntry(string AuthorName, string Comment)
{
    public const int AuthorNameBytes = 37;
    public const int CommentBytes = 385;

    internal void Write(PacketWriter writer)
    {
        writer.WriteFixedString(AuthorName, AuthorNameBytes);
        writer.WriteFixedString(Comment, CommentBytes);
    }
}
