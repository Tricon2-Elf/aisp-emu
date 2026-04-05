using System.Buffers.Binary;
using System.Text;
using AISpace.Network;
using AISpace.Network.Packets.Auth;
using AISpace.Network.Packets.Common;

namespace AISpace.Network.Tests;

public class PacketRoundTripTests
{
    [Fact]
    public void PingRequest_RoundTrip()
    {
        var original = new PingRequest(0xDEADBEEF);
        var w = new PacketWriter();
        w.Write(original.Time);
        var bytes = w.ToBytes();
        var round = PingRequest.FromBytes(bytes);
        Assert.Equal(original.Time, round.Time);
        var w2 = new PacketWriter();
        w2.Write(round.Time);
        Assert.Equal(bytes, w2.ToBytes());
    }

    [Fact]
    public void LoginRequest_FromBytes_ReadsUserIdAndOtp()
    {
        var w = new PacketWriter();
        w.Write(42u);
        var otp = new byte[20];
        Encoding.ASCII.GetBytes("01234567890123456789").CopyTo(otp, 0);
        w.Write(otp);

        var req = LoginRequest.FromBytes(w.ToBytes());
        Assert.Equal(42u, req._userId);
        Assert.Equal("01234567890123456789", Encoding.ASCII.GetString(req._otp));
    }

    [Fact]
    public void AuthenticateRequest_FromBytes_ReadsCredentials()
    {
        var w = new PacketWriter();
        w.Write("user");
        w.Write("secret");
        var req = AuthenticateRequest.FromBytes(w.ToBytes());
        Assert.Equal("user", req.Username);
        Assert.Equal("secret", req.Password);
    }

    [Fact]
    public void WorldSelectRequest_FromBytes_ReadsWorldId()
    {
        var w = new PacketWriter();
        w.Write(7u);
        var req = WorldSelectRequest.FromBytes(w.ToBytes());
        Assert.Equal(7u, req.WorldID);
    }
}
