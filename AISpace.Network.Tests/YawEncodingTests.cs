using AISpace.Network;

namespace AISpace.Network.Tests;

public class YawEncodingTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(180, 90)]
    [InlineData(90, 45)]
    [InlineData(360, 0)]
    [InlineData(-120, 120)]
    public void ToWireByte_MapsDegreesToHalfDegrees(int degrees, byte expectedWire)
    {
        Assert.Equal(expectedWire, YawEncoding.ToWireByte(degrees));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, 180)]
    [InlineData(45, 90)]
    [InlineData(136, 272)]
    public void FromWireByte_MapsHalfDegreesToDegrees(byte wire, int expectedDegrees)
    {
        Assert.Equal(expectedDegrees, YawEncoding.FromWireByte(wire));
    }

    [Fact]
    public void RoundTrip_EvenDegrees_PreservesFacing()
    {
        Assert.Equal(180, YawEncoding.FromWireByte(YawEncoding.ToWireByte(180)));
        Assert.Equal(90, YawEncoding.FromWireByte(YawEncoding.ToWireByte(90)));
        Assert.Equal(0, YawEncoding.FromWireByte(YawEncoding.ToWireByte(0)));
    }
}
