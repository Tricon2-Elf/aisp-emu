using aisp.Network.Data;
using aisp.Network.Packets.Msg;

namespace aisp.Network.Tests;

public class MailPacketTests
{
    [Fact]
    public void MailData_WireSize_Is960()
    {
        var data = new MailData
        {
            MailId = 1,
            Subject = "hi",
            Body = "body",
        };
        Assert.Equal(MailData.WireSize, data.ToBytes().Length);
    }

    [Fact]
    public void MailData_RoundTrip()
    {
        var original = new MailData
        {
            MailId = 0x1_0000_0002UL,
            Type = 1,
            Flags = 0,
            SenderId = 42,
            SenderName = "送信者",
            DistId = 7,
            DistName = "宛先",
            Date = "2026/08/25 02:13:21",
            Subject = "[題]",
            Body = "Hello World!",
        };

        var bytes = original.ToBytes();
        Assert.Equal(MailData.WireSize, bytes.Length);

        var parsed = MailData.FromBytes(bytes);
        Assert.Equal(original.MailId, parsed.MailId);
        Assert.Equal(original.Type, parsed.Type);
        Assert.Equal(original.Flags, parsed.Flags);
        Assert.Equal(original.SenderId, parsed.SenderId);
        Assert.Equal(original.SenderName, parsed.SenderName);
        Assert.Equal(original.DistId, parsed.DistId);
        Assert.Equal(original.DistName, parsed.DistName);
        Assert.Equal(original.Date, parsed.Date);
        Assert.Equal(original.Subject, parsed.Subject);
        Assert.Equal(original.Body, parsed.Body);
    }

    [Fact]
    public void MailPostRequest_FromBytes_MatchesClientCapture()
    {
        // Raw payload from client send_post_mail (27 bytes).
        byte[] payload =
        [
            0x01,
            0x00,
            0x00,
            0x00,
            0x00,
            0x5B,
            0xE7,
            0x84,
            0xA1,
            0xE9,
            0xA1,
            0x8C,
            0x5D,
            0x00,
            0x48,
            0x65,
            0x6C,
            0x6C,
            0x6F,
            0x20,
            0x57,
            0x6F,
            0x72,
            0x6C,
            0x64,
            0x21,
            0x00,
        ];

        var req = MailPostRequest.FromBytes(payload);
        Assert.Equal(1u, req.DistId);
        Assert.Equal(string.Empty, req.DistName);
        Assert.Equal("[無題]", req.Subject);
        Assert.Equal("Hello World!", req.Body);
    }

    [Fact]
    public void MailPostResponse_ToBytes_IsResultPlusMailData()
    {
        var mail = new MailData
        {
            MailId = 99,
            Subject = "s",
            Body = "b",
        };
        var bytes = new MailPostResponse(0, mail).ToBytes();
        Assert.Equal(sizeof(uint) + MailData.WireSize, bytes.Length);

        var reader = new PacketReader(bytes);
        Assert.Equal(0u, reader.ReadUInt());
        var parsed = MailData.Read(ref reader);
        Assert.Equal(99UL, parsed.MailId);
        Assert.Equal("s", parsed.Subject);
        Assert.Equal("b", parsed.Body);
    }

    [Fact]
    public void MailPostRequest_RoundTrip_NullTerminatedStrings()
    {
        var writer = new PacketWriter();
        writer.Write(123u);
        writer.Write("Alice", 36);
        writer.Write("Subject", 90);
        writer.Write("Body text", 750);

        var req = MailPostRequest.FromBytes(writer.ToBytes());
        Assert.Equal(123u, req.DistId);
        Assert.Equal("Alice", req.DistName);
        Assert.Equal("Subject", req.Subject);
        Assert.Equal("Body text", req.Body);
    }

    [Fact]
    public void MailOpenRequest_FromBytes_MatchesClientCapture()
    {
        // F2-DD-BF-36-A0-01-00-00-00-00-00-00
        byte[] payload = [0xF2, 0xDD, 0xBF, 0x36, 0xA0, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        var req = MailOpenRequest.FromBytes(payload);
        Assert.Equal(0x01A036BFDDF2UL, req.MailId);
        Assert.Equal(0u, req.Type);
    }

    [Fact]
    public void MailOpenResponse_RoundTrip()
    {
        var bytes = new MailOpenResponse(0, 0x01A036BFDDF2UL, 0).ToBytes();
        Assert.Equal(16, bytes.Length);

        var reader = new PacketReader(bytes);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0x01A036BFDDF2UL, reader.ReadULong());
        Assert.Equal(0u, reader.ReadUInt());
    }
}
