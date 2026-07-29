using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.Tests;

public class DistributedStateRepositoriesTests
{
    [Fact]
    public async Task SessionPresenceRepository_UpsertArea_ReplacesGhostCharacter()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var factory = new TestMainContextFactory(options);
            var repository = new SessionPresenceRepository(factory);
            var first = new CapturingPlayerSession
            {
                UserId = 7,
                CharacterId = 77,
                MapId = 10990100,
                ChannelId = 1,
            };
            var second = new CapturingPlayerSession
            {
                UserId = 7,
                CharacterId = 77,
                MapId = 10990110,
                ChannelId = 2,
            };

            repository.Upsert(ServerType.Area, first);
            repository.Upsert(ServerType.Area, second);

            var sessions = repository.GetByServerType(ServerType.Area);
            Assert.Single(sessions);
            Assert.Equal(second.ConnectionId, sessions[0].ConnectionId);
            Assert.Equal(10990110u, sessions[0].MapId);
            Assert.Equal(2, sessions[0].ChannelId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PendingMapTransferRepository_TryTake_IsConsumeOnce()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var factory = new TestMainContextFactory(options);
            var repository = new PendingMapTransferRepository(factory);
            var expected = new SharedState.PendingMapTransfer(12, 10990110, 1, -1f, 2f, 3f, 4);

            repository.Upsert(expected, TimeSpan.FromMinutes(5));

            var firstTake = repository.TryTake(12, out var firstTransition);
            var secondTake = repository.TryTake(12, out _);

            Assert.True(firstTake);
            Assert.False(secondTake);
            Assert.Equal(expected.MapId, firstTransition.MapId);
            Assert.Equal(expected.ChannelId, firstTransition.ChannelId);

            await using var verify = new MainContext(options);
            Assert.False(
                await verify.PendingMapTransfers.AnyAsync(
                    row => row.UserId == 12,
                    TestContext.Current.CancellationToken
                )
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
