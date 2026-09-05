using aisp.Common.Game;
using aisp.Common.Handlers.Msg;
using aisp.Common.Localisation;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Tests;

public sealed class GetPlacardCommentLogHandlerTests
{
    [Theory]
    [InlineData(GameLanguage.Japanese, "コメントはありません。")]
    [InlineData(GameLanguage.English, "No comments")]
    [InlineData(GameLanguage.ChineseSimplified, "暂无评论")]
    [InlineData(GameLanguage.ChineseTraditional, "暫無評論")]
    public async Task EmptyLog_ReturnsLocalisedNoCommentsMessageWithoutDefaultAuthor(
        GameLanguage language,
        string expected
    )
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
        var session = new CapturingPlayerSession { UserId = 4, Language = language };
        var request = new PacketWriter();
        request.Write(placard.PlacardId);

        await new GetPlacardCommentLogHandler(state, TestTextLocaliser.English).HandleAsync(
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
        Assert.Equal(expected, reader.ReadFixedString(PlacardCommentLogEntry.CommentBytes));
    }
}
