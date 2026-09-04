using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Network.Tests;

public sealed class PlacardCommentPacketTests
{
    [Fact]
    public void CommentNotification_UsesTheClientFixedRecordLayout()
    {
        var bytes = new NotifyPlacardCommentLog(
            0,
            4,
            [new PlacardCommentLogEntry("AISpace", "Welcome!")]
        ).ToBytes();
        var reader = new PacketReader(bytes);

        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal("AISpace", reader.ReadFixedString(PlacardCommentLogEntry.AuthorNameBytes));
        Assert.Equal("Welcome!", reader.ReadFixedString(PlacardCommentLogEntry.CommentBytes));
    }
}
