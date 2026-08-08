using AISpace.Network.Data;
using AISpace.Network.Packets.Msg;

namespace AISpace.Network.Tests;

public class CirclePacketTests
{
    [Fact]
    public void CircleData_WireSize_Is866()
    {
        var data = new CircleData(7, "TestCircle", 42)
        {
            Status = 1,
            Mark = "1",
            Date = "2026-08-08",
            Message = "hello",
        };
        Assert.Equal(CircleData.WireSize, data.ToBytes().Length);
    }

    [Fact]
    public void CircleData_RoundTrip()
    {
        var original = new CircleData(12, "アルファ", 99)
        {
            Status = 1,
            Mark = "mark",
            Date = "08/08",
            Message = "掲示板メッセージ",
        };
        var bytes = original.ToBytes();
        var reader = new PacketReader(bytes);
        var parsed = CircleData.Read(ref reader);
        Assert.Equal(original.Id, parsed.Id);
        Assert.Equal(original.Status, parsed.Status);
        Assert.Equal(original.Name, parsed.Name);
        Assert.Equal(original.LeaderId, parsed.LeaderId);
        Assert.Equal(original.Mark, parsed.Mark);
        Assert.Equal(original.Date, parsed.Date);
        Assert.Equal(original.Message, parsed.Message);
    }

    [Fact]
    public void CircleChatForwardNotify_EncodesUtf8NotShiftJis()
    {
        const string message = "こんにちは";
        var bytes = new CircleChatForwardNotify(1, message).ToBytes();
        var reader = new PacketReader(bytes);
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(message, reader.ReadString("utf-8"));

        var utf8 = System.Text.Encoding.UTF8.GetBytes(message + "\0");
        var sjis = System.Text.Encoding.GetEncoding(932).GetBytes(message + "\0");
        Assert.NotEqual(utf8, sjis);
        Assert.Equal(utf8, bytes.AsSpan(4).ToArray());
    }

    [Fact]
    public void CircleMemberData_WireSize_Is48()
    {
        var member = new CircleMemberData
        {
            AvatarId = 5,
            Name = "Bob",
            Role = CircleMemberData.RoleLeader,
        };
        Assert.Equal(CircleMemberData.WireSize, member.ToBytes().Length);
    }

    [Fact]
    public void CircleCreateRequest_FromBytes()
    {
        var writer = new PacketWriter();
        writer.Write("CircleA", "utf-8");
        writer.Write(3u);
        var req = CircleCreateRequest.FromBytes(writer.ToBytes());
        Assert.Equal("CircleA", req.Name);
        Assert.Equal(3u, req.MarkId);
    }

    [Fact]
    public void CircleCreateResponse_IncludesCircleData()
    {
        var resp = new CircleCreateResponse(0, new CircleData(1, "C", 2));
        Assert.Equal(4 + CircleData.WireSize, resp.ToBytes().Length);
    }

    [Fact]
    public void CircleChatInRequest_ReadsULong()
    {
        var writer = new PacketWriter();
        writer.Write(123UL);
        var req = CircleChatInRequest.FromBytes(writer.ToBytes());
        Assert.Equal(123UL, req.CircleId);
    }

    [Fact]
    public void CircleChatInResponse_HasCountAndAvatarIds()
    {
        var resp = new CircleChatInResponse(0, 1, [10, 20]);
        var bytes = resp.ToBytes();
        var reader = new PacketReader(bytes);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(10u, reader.ReadUInt());
        Assert.Equal(20u, reader.ReadUInt());
    }

    [Fact]
    public void CircleChatPostRequest_IsMessageIdAndText()
    {
        var writer = new PacketWriter();
        writer.Write(9u);
        writer.Write("hi", "utf-8");
        var req = CircleChatPostRequest.FromBytes(writer.ToBytes());
        Assert.Equal(9u, req.MessageId);
        Assert.Equal("hi", req.Message);
    }

    [Fact]
    public void CircleChatForwardNotify_IsFromIdAndMessage()
    {
        var bytes = new CircleChatForwardNotify(55, "yo").ToBytes();
        var reader = new PacketReader(bytes);
        Assert.Equal(55u, reader.ReadUInt());
        Assert.Equal("yo", reader.ReadString("utf-8"));
    }

    [Fact]
    public void CircleNotifyMember_WritesULongCircleId()
    {
        CircleMemberData[] members =
        [
            new()
            {
                AvatarId = 1,
                Name = "A",
                Role = CircleMemberData.RoleLeader,
            },
        ];
        var bytes = new CircleNotifyMember(7UL, members, [true]).ToBytes();
        var reader = new PacketReader(bytes);
        Assert.Equal(7UL, reader.ReadULong());
        Assert.Equal(1u, reader.ReadUInt());
        var member = CircleMemberData.Read(ref reader);
        Assert.Equal(1u, member.AvatarId);
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(1, reader.ReadByte());
    }

    [Fact]
    public void CircleNotifyKick_IsCircleIdOnly()
    {
        var bytes = new CircleNotifyKick(99UL).ToBytes();
        Assert.Equal(8, bytes.Length);
    }

    [Fact]
    public void CircleGetDataResponse_IncludesAuthLevels()
    {
        (CircleData, uint)[] memberships =
        [
            (new CircleData(1, "One", 10), CircleMemberData.RoleLeader),
            (new CircleData(2, "Two", 20), CircleMemberData.RoleMember),
        ];
        var bytes = new CircleGetDataResponse(0, memberships).ToBytes();
        var reader = new PacketReader(bytes);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(1u, CircleData.Read(ref reader).Id);
        Assert.Equal(2u, CircleData.Read(ref reader).Id);
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(CircleMemberData.RoleLeader, reader.ReadUInt());
        Assert.Equal(CircleMemberData.RoleMember, reader.ReadUInt());
    }

    [Fact]
    public void CircleMemberJoinMemberRequest_ParsesTargetAndCircle()
    {
        var writer = new PacketWriter();
        writer.Write(44u);
        writer.Write(8UL);
        var req = CircleMemberJoinMemberRequest.FromBytes(writer.ToBytes());
        Assert.Equal(44u, req.TargetAvatarId);
        Assert.Equal(8UL, req.CircleId);
    }

    [Fact]
    public void CircleMessageChangeRequest_ParsesMessage()
    {
        var writer = new PacketWriter();
        writer.Write(3UL);
        writer.Write("board", "utf-8");
        var req = CircleMessageChangeRequest.FromBytes(writer.ToBytes());
        Assert.Equal(3UL, req.CircleId);
        Assert.Equal("board", req.Message);
    }
}
