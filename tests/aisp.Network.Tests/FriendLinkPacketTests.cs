using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public sealed class FriendLinkPacketTests
{
    [Fact]
    public void TagLookupResponse_WritesAllFourBoundedCollections()
    {
        var bytes = new FriendLinkTagGetResponse(
            0,
            42,
            [new FriendLinkTagData(7, "Close friend")],
            [3],
            [new FriendLinkTagData(9, "Questionnaire")],
            [4]
        ).ToBytes();
        var reader = new PacketReader(bytes);

        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(42u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(7u, reader.ReadUInt());
        Assert.Equal("Close friend", reader.ReadFixedString(FriendLinkTagData.NameBytes));
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(9u, reader.ReadUInt());
        Assert.Equal("Questionnaire", reader.ReadFixedString(FriendLinkTagData.NameBytes));
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
    }

    [Fact]
    public void TagChangeRequest_ReadsSlotAndNullTerminatedName()
    {
        var writer = new PacketWriter();
        writer.Write(2u);
        writer.Write("Best friends");

        var packet = FriendLinkTagChangeRequest.FromBytes(writer.ToBytes());

        Assert.Equal(2u, packet.Slot);
        Assert.Equal("Best friends", packet.Name);
    }

    [Fact]
    public void TagChangeResponse_WritesThePositiveTagIdExpectedByTheClient()
    {
        var reader = new PacketReader(new FriendLinkResultResponse(5).ToBytes());

        Assert.Equal(5u, reader.ReadUInt());
    }

    [Fact]
    public void FreeTagResponse_WritesTwoExactClientTagRecords()
    {
        var bytes = new GetFreeFriendLinkTagResponse(
            0,
            [
                new FriendLinkTagData(100001, "Test tag one"),
                new FriendLinkTagData(100002, "Test tag two"),
            ]
        ).ToBytes();
        var reader = new PacketReader(bytes);

        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(100001u, reader.ReadUInt());
        Assert.Equal("Test tag one", reader.ReadFixedString(FriendLinkTagData.NameBytes));
        Assert.Equal(100002u, reader.ReadUInt());
        Assert.Equal("Test tag two", reader.ReadFixedString(FriendLinkTagData.NameBytes));
        Assert.Equal(138, bytes.Length);
    }
}
