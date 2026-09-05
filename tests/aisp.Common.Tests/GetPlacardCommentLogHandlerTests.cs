using aisp.Common.Game;
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
        var state = new SharedState();
        var (placard, _) = state.SetFriendLinkPlacard(
            4,
            4,
            "Owner",
            1,
            1,
            0,
            1,
            0,
            0,
            "Anime",
            default
        );
        var session = new CapturingPlayerSession { UserId = 4 };
        var request = new PacketWriter();
        request.Write(placard.PlacardId);

        await new GetPlacardCommentLogHandler(state).HandleAsync(
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
        Assert.Equal(placard.PlacardId, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(string.Empty, reader.ReadFixedString(PlacardCommentLogEntry.AuthorNameBytes));
        Assert.Equal("No comments", reader.ReadFixedString(PlacardCommentLogEntry.CommentBytes));
    }
}
