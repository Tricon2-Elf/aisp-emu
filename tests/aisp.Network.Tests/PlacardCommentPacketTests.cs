using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Network.Tests;

public sealed class PlacardCommentPacketTests
{
    [Fact]
    public void EmptyCommentMessage_UsesTheClientFixedRecordLayout()
    {
        var bytes = new NotifyPlacardCommentLog(
            0,
            4,
            [new PlacardCommentLogEntry(string.Empty, "No comments")]
        ).ToBytes();
        var reader = new PacketReader(bytes);

        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(string.Empty, reader.ReadFixedString(PlacardCommentLogEntry.AuthorNameBytes));
        Assert.Equal("No comments", reader.ReadFixedString(PlacardCommentLogEntry.CommentBytes));
    }
}
