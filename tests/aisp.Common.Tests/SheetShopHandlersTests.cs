using aisp.Common.Config;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace aisp.Common.Tests;

public sealed class SheetShopHandlersTests
{
    private static byte[] BuyBytes(uint count, long price)
    {
        var writer = new PacketWriter();
        writer.Write(count);
        writer.Write((ulong)price);
        return writer.ToBytes();
    }

    [Fact]
    public async Task Start_Buy_End_RunTheSheetShopWindow()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var user = new User
            {
                Username = "author",
                AiPoints = 100,
                AdventureSheetStock = 5,
            };
            user.SetPassword("pw");
            db.Users.Add(user);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            var serverOptions = Options.Create(new ServerOptions { AdventureSheetPriceAi = 10 });
            var session = new CapturingPlayerSession
            {
                UserId = user.Id,
                User = user,
                CharacterId = 1,
            };

            await new AreaSheetShopStartHandler(serverOptions).HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );
            var start = Assert.Single(session.Sent);
            Assert.Equal(PacketType.SheetShopStartResponse, start.Type);
            Assert.Equal(12, start.Payload.Length);
            var sr = new PacketReader(start.Payload);
            Assert.Equal(0u, sr.ReadUInt());
            Assert.Equal(10ul, sr.ReadULong());

            var buy = new AreaSheetShopBuyHandler(
                new AdventureWorkRepository(db),
                serverOptions,
                NullLogger<AreaSheetShopBuyHandler>.Instance
            );
            session.Sent.Clear();
            await buy.HandleAsync(BuyBytes(7, 10), session, TestContext.Current.CancellationToken);
            // Stock and purse pushes precede the 4-byte result: the window redraws from the stored stock on it.
            Assert.Equal(
                [
                    PacketType.AdventureUpdatedSheetStackNotify,
                    PacketType.MoneyUpdatedAipoint,
                    PacketType.SheetShopBuyResponse,
                ],
                session.Sent.Select(p => p.Type)
            );
            Assert.Equal(12u, new PacketReader(session.Sent[0].Payload).ReadUInt());
            Assert.Equal(30ul, new PacketReader(session.Sent[1].Payload).ReadULong());
            Assert.Equal([0, 0, 0, 0], session.Sent[2].Payload);
            Assert.Equal(30, user.AiPoints);

            // Too expensive now: the client's own short-purse code, purse untouched.
            session.Sent.Clear();
            await buy.HandleAsync(BuyBytes(4, 10), session, TestContext.Current.CancellationToken);
            var refused = Assert.Single(session.Sent);
            Assert.Equal(
                AreaSheetShopBuyHandler.NotEnoughDereResult,
                new PacketReader(refused.Payload).ReadUInt()
            );
            Assert.Equal(30, user.AiPoints);

            // A price that is not the configured one is refused before touching anything.
            session.Sent.Clear();
            await buy.HandleAsync(BuyBytes(1, 1), session, TestContext.Current.CancellationToken);
            Assert.Equal(
                AreaSheetShopBuyHandler.RefusedResult,
                new PacketReader(Assert.Single(session.Sent).Payload).ReadUInt()
            );

            session.Sent.Clear();
            await new AreaSheetShopEndHandler().HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(PacketType.SheetShopEndResponse, Assert.Single(session.Sent).Type);
            Assert.Equal([0, 0, 0, 0], session.Sent[0].Payload);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
