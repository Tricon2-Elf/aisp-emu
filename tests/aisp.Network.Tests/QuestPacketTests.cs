using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public class QuestPacketTests
{
    [Fact]
    public void QuestWorkData_RoundTrip_Is1147Bytes()
    {
        var original = SampleWork();
        var bytes = original.ToBytes();
        Assert.Equal(QuestWorkData.WireSize, bytes.Length);

        var parsed = QuestWorkData.FromBytes(bytes);
        Assert.Equal(original.Base.QuestId, parsed.Base.QuestId);
        Assert.Equal(original.Base.Title, parsed.Base.Title);
        Assert.Equal(original.Base.ShortName, parsed.Base.ShortName);
        Assert.Equal(original.Base.Chapter, parsed.Base.Chapter);
        Assert.Equal(original.Base.Note, parsed.Base.Note);
        Assert.Equal(original.RestSec, parsed.RestSec);
        Assert.Equal(original.LocationName, parsed.LocationName);
        Assert.Equal(original.TargetName, parsed.TargetName);
        Assert.Equal(original.Now, parsed.Now);
        Assert.Equal(original.Required, parsed.Required);
    }

    [Fact]
    public void QuestHistoryData_RoundTrip_Is1010Bytes()
    {
        var original = new QuestHistoryData
        {
            Base = SampleBase(),
            RelatedQuestId = 0,
            Result = 1,
        };
        var bytes = original.ToBytes();
        Assert.Equal(QuestHistoryData.WireSize, bytes.Length);

        var parsed = QuestHistoryData.FromBytes(bytes);
        Assert.Equal(original.Base.QuestId, parsed.Base.QuestId);
        Assert.Equal(original.RelatedQuestId, parsed.RelatedQuestId);
        Assert.Equal(original.Result, parsed.Result);
    }

    [Fact]
    public void QuestGetWorkResponse_RoundTrip()
    {
        var response = new QuestGetWorkResponse(0, [SampleWork()]);
        var parsed = QuestGetWorkResponse.FromBytes(response.ToBytes());
        Assert.Equal(0u, parsed.Result);
        var quest = Assert.Single(parsed.Quests);
        Assert.Equal(1u, quest.Base.QuestId);
        Assert.Equal("Welcome Quest", quest.Base.Title);
    }

    [Fact]
    public void QuestSetTargetNotify_UsesNullTerminatedName()
    {
        var notify = new QuestSetTargetNotify(1, 1, "Take a first look around");
        var parsed = QuestSetTargetNotify.FromBytes(notify.ToBytes());
        Assert.Equal(1u, parsed.QuestId);
        Assert.Equal((ushort)1, parsed.Required);
        Assert.Equal("Take a first look around", parsed.TargetName);
    }

    [Fact]
    public void EventQuestSelectExecNotify_RoundTrip()
    {
        var notify = new EventQuestSelectExecNotify("Pick a quest", [SampleBase()]);
        var parsed = EventQuestSelectExecNotify.FromBytes(notify.ToBytes());
        Assert.Equal("Pick a quest", parsed.Text);
        var quest = Assert.Single(parsed.Quests);
        Assert.Equal(1u, quest.QuestId);
        Assert.Equal("Welcome Quest", quest.Title);
    }

    [Fact]
    public void EventQuestSelectExecRRequest_ReadsResultAndSelectId()
    {
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.Write(3u);
        var request = EventQuestSelectExecRRequest.FromBytes(writer.ToBytes());
        Assert.Equal(0u, request.Result);
        Assert.Equal(3u, request.SelectId);
    }

    private static QuestBaseData SampleBase() =>
        new()
        {
            QuestId = 1,
            Title = "Welcome Quest",
            ShortName = "Welcome",
            Chapter = 1,
            Note = "You connected to the area.",
        };

    private static QuestWorkData SampleWork() =>
        new()
        {
            Base = SampleBase(),
            RestSec = 0,
            LocationName = "Area",
            TargetName = "Take a first look around",
            Now = 0,
            Required = 1,
        };
}
