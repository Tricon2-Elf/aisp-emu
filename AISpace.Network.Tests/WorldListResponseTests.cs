using System.Buffers.Binary;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Auth;

namespace AISpace.Network.Tests;

public class WorldListResponseTests
{
    [Fact]
    public void WorldListResponse_ToBytes_HasExpectedHeaderAndLength()
    {
        var worlds = new List<WorldData>
        {
            new()
            {
                Id = 1,
                Name = "w1",
                Description = "d1",
                Address = "127.0.0.1",
                Port = 50052,
            },
        };
        var bytes = new WorldListResponse(0, worlds).ToBytes();
        Assert.True(bytes.Length >= 8);
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0, 4)));
        Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4, 4)));
    }
}
