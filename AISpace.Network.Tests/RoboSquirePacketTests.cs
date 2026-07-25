using AISpace.Network.Packets.Area;

namespace AISpace.Network.Tests;

public class RoboSquirePacketTests
{
    [Fact]
    public void Request_FromBytes_ReadsRoboId()
    {
        var writer = new PacketWriter();
        writer.Write(7u);

        var request = RoboSquireRequest.FromBytes(writer.ToBytes());

        Assert.Equal(7u, request.RoboId);
    }

    [Fact]
    public void Response_ToBytes_WritesRoboIdAndResult()
    {
        var bytes = new RoboSquireResponse(7, 0).ToBytes();

        Assert.Equal(8, bytes.Length);
        var reader = new PacketReader(bytes);
        Assert.Equal(7u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
    }
}
