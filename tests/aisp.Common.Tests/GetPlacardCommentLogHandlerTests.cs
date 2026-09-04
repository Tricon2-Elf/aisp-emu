using aisp.Common.Handlers.Msg;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Tests;

public sealed class GetPlacardCommentLogHandlerTests
{
    [Fact]
    public async Task EmptyLog_ReturnsNoCommentsMessageWithoutDefaultAuthor()
    {
        var session = new CapturingPlayerSession();
        var request = new PacketWriter();
        request.Write(4u);

        await new GetPlacardCommentLogHandler().HandleAsync(
            request.ToBytes(),
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(2, session.Sent.Count);
        Assert.Equal(PacketType.GetPlacardCommentLogResponse, session.Sent[0].Type);
        Assert.Equal(0u, new PacketReader(session.Sent[0].Payload).ReadUInt());

        Assert.Equal(PacketType.NotifyPlacardCommentLog, session.Sent[1].Type);
        var reader = new PacketReader(session.Sent[1].Payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(string.Empty, reader.ReadFixedString(PlacardCommentLogEntry.AuthorNameBytes));
        Assert.Equal("No comments", reader.ReadFixedString(PlacardCommentLogEntry.CommentBytes));
    }
}
