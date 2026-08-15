using System.Buffers.Binary;
using aisp.Common.DAL.Entities;
using aisp.Common.Handlers.Auth;
using aisp.Common.Tests.Support;
using aisp.Network;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace aisp.Common.Tests;

public class WorldListHandlerTests
{
    [Fact]
    public async Task SendsWorldListResponse_WithRepositoryWorlds()
    {
        var worlds = new List<World>
        {
            new()
            {
                Id = 1,
                Name = "w",
                Description = "d",
                Address = "host",
                Port = 50052,
            },
        };

        var repo = new MockWorldRepository(worlds);
        var handler = new WorldListHandler(
            repo,
            TestTextLocaliser.English,
            NullLogger<WorldListHandler>.Instance
        );
        var session = new CapturingPlayerSession();

        await handler.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Single(session.Sent);
        Assert.Equal(PacketType.WorldListResponse, session.Sent[0].Type);
        var payload = session.Sent[0].Payload;
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, 4)));
        Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(4, 4)));
    }

    private sealed class MockWorldRepository(List<World> worlds)
        : aisp.Common.DAL.Repositories.IWorldRepository
    {
        public Task AddAsync(string name, string description, string address, ushort port) =>
            Task.CompletedTask;

        public Task<World?> GetByIdAsync(int id) =>
            Task.FromResult<World?>(worlds.FirstOrDefault(w => w.Id == id));

        public Task<World?> GetByNameAsync(string name) =>
            Task.FromResult<World?>(worlds.FirstOrDefault(w => w.Name == name));

        public Task<List<World>> GetAllAsync() => Task.FromResult(worlds);
    }
}
