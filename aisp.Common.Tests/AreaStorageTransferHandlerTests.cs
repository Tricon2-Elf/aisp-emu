using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class AreaStorageTransferHandlerTests
{
    [Fact]
    public async Task Withdraw_MovesDepositToPurse_AndNotifiesBalances()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var user = new User
            {
                Username = "storage-user",
                AiPoints = 100,
                StorageDeposit = 50,
            };
            user.SetPassword("password");
            db.Users.Add(user);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var session = new CapturingPlayerSession
            {
                User = new User
                {
                    Id = user.Id,
                    AiPoints = user.AiPoints,
                    StorageDeposit = user.StorageDeposit,
                },
            };
            var handler = new AreaStorageWithdrawHandler(
                new UserRepository(db),
                NullLogger<AreaStorageWithdrawHandler>.Instance
            );

            await handler.HandleAsync(
                BitConverter.GetBytes(12UL),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(112, session.User!.AiPoints);
            Assert.Equal(38, session.User.StorageDeposit);

            var response = Assert.Single(
                session.Sent,
                packet => packet.Type == PacketType.StorageWithdrawResponse
            );
            var reader = new PacketReader(response.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(38ul, reader.ReadULong());

            var purse = Assert.Single(
                session.Sent,
                packet => packet.Type == PacketType.MoneyUpdatedAipoint
            );
            Assert.Equal(112ul, new PacketReader(purse.Payload).ReadULong());

            var deposit = Assert.Single(
                session.Sent,
                packet => packet.Type == PacketType.StorageUpdatedDepositNotify
            );
            Assert.Equal(38ul, new PacketReader(deposit.Payload).ReadULong());

            var persisted = await db.Users.FindAsync(
                [user.Id],
                TestContext.Current.CancellationToken
            );
            Assert.Equal(112, persisted!.AiPoints);
            Assert.Equal(38, persisted.StorageDeposit);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Deposit_MovesPurseToDeposit_AndNotifiesBalances()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var user = new User
            {
                Username = "storage-user-2",
                AiPoints = 100,
                StorageDeposit = 50,
            };
            user.SetPassword("password");
            db.Users.Add(user);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var session = new CapturingPlayerSession
            {
                User = new User
                {
                    Id = user.Id,
                    AiPoints = user.AiPoints,
                    StorageDeposit = user.StorageDeposit,
                },
            };
            var handler = new AreaStorageDepositHandler(
                new UserRepository(db),
                NullLogger<AreaStorageDepositHandler>.Instance
            );

            await handler.HandleAsync(
                BitConverter.GetBytes(20UL),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(80, session.User!.AiPoints);
            Assert.Equal(70, session.User.StorageDeposit);

            var response = Assert.Single(
                session.Sent,
                packet => packet.Type == PacketType.StorageDepositResponse
            );
            var reader = new PacketReader(response.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(70ul, reader.ReadULong());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Withdraw_RejectsWhenDepositInsufficient()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var user = new User
            {
                Username = "storage-user-3",
                AiPoints = 100,
                StorageDeposit = 5,
            };
            user.SetPassword("password");
            db.Users.Add(user);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var session = new CapturingPlayerSession
            {
                User = new User
                {
                    Id = user.Id,
                    AiPoints = user.AiPoints,
                    StorageDeposit = user.StorageDeposit,
                },
            };
            var handler = new AreaStorageWithdrawHandler(
                new UserRepository(db),
                NullLogger<AreaStorageWithdrawHandler>.Instance
            );

            await handler.HandleAsync(
                BitConverter.GetBytes(12UL),
                session,
                TestContext.Current.CancellationToken
            );

            var response = Assert.Single(
                session.Sent,
                packet => packet.Type == PacketType.StorageWithdrawResponse
            );
            Assert.Equal(1u, new PacketReader(response.Payload).ReadUInt());
            Assert.DoesNotContain(
                session.Sent,
                packet => packet.Type == PacketType.MoneyUpdatedAipoint
            );
            Assert.Equal(100, session.User!.AiPoints);
            Assert.Equal(5, session.User.StorageDeposit);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
