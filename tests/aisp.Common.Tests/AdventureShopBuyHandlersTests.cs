using aisp.Common.Config;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace aisp.Common.Tests;

public sealed class AdventureShopBuyHandlersTests
{
    private static async Task<(User Author, User Buyer, AdventureListing Listing)> SeedAsync(
        MainContext db
    )
    {
        var author = new User { Username = "author" };
        author.SetPassword("pw");
        var buyer = new User { Username = "buyer", AiPoints = 250 };
        buyer.SetPassword("pw");
        db.Users.AddRange(author, buyer);
        await db.SaveChangesAsync();
        await new AdventureWorkRepository(db).RegisterAsync(author.Id, 1, 4, 2);
        var shop = new AdventureShopRepository(db);
        var started = await shop.BeginUploadAsync(
            author.Id,
            1,
            4,
            new AdventureListingDraft(
                "Thimbleglow",
                "Yomogi",
                2,
                "A silly story",
                100,
                false,
                20348
            )
        );
        await shop.RedeemUploadTicketAsync(started!.Value.Ticket);
        await shop.StoreContentAsync(started.Value.Listing.ScriptId, "ADV0"u8.ToArray(), []);
        var listing = await shop.ConfirmUploadAsync(author.Id, started.Value.Listing.ScriptId);
        return (author, buyer, listing!);
    }

    private static byte[] BuyBytes(long scriptId, long price, byte priceType)
    {
        var writer = new PacketWriter();
        writer.Write((ulong)scriptId);
        writer.Write((ulong)price);
        writer.Write(priceType);
        return writer.ToBytes();
    }

    private static byte[] ScriptIdBytes(long scriptId)
    {
        var writer = new PacketWriter();
        writer.Write((ulong)scriptId);
        return writer.ToBytes();
    }

    [Fact]
    public async Task Buy_PushesHistoryTicketAndAck_ThenRefusesWithinSevenDays()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var (_, buyer, listing) = await SeedAsync(db);
            var shop = new AdventureShopRepository(db);
            var handler = new AreaAdventureShopBuyHandler(
                shop,
                Options.Create(new ServerOptions { AdventureUploadRatePercent = 70 }),
                NullLogger<AreaAdventureShopBuyHandler>.Instance
            );
            var session = new CapturingPlayerSession
            {
                UserId = buyer.Id,
                User = buyer,
                CharacterId = 2,
            };

            await handler.HandleAsync(
                BuyBytes(listing.ScriptId, 100, 0),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(
                [
                    PacketType.AdventureShopAddedBuyHistoryNotify,
                    PacketType.MoneyUpdatedAipoint,
                    PacketType.AdventureShopBuyResponse,
                ],
                session.Sent.Select(p => p.Type)
            );
            Assert.Equal(1594, session.Sent[0].Payload.Length);
            Assert.Equal(150ul, new PacketReader(session.Sent[1].Payload).ReadULong());
            Assert.Equal(150, buyer.AiPoints);
            Assert.Equal([0, 0, 0, 0], session.Sent[2].Payload);
            // The client asks for the download itself; the ticket it gets really downloads the disc.
            var ticket = await shop.IssueDownloadTicketAsync(buyer.Id, listing.ScriptId);
            Assert.NotNull(ticket);
            var content = await shop.RedeemDownloadTicketAsync(ticket);
            Assert.NotNull(content);
            Assert.Equal("ADV0"u8.ToArray(), content.Script);

            // A second purchase inside the 7-day window is refused with the ack alone.
            session.Sent.Clear();
            await handler.HandleAsync(
                BuyBytes(listing.ScriptId, 100, 0),
                session,
                TestContext.Current.CancellationToken
            );
            var refusal = Assert.Single(session.Sent);
            Assert.Equal(PacketType.AdventureShopBuyResponse, refusal.Type);
            Assert.Equal(
                (uint)AdventureBuyOutcome.AlreadyOwned,
                new PacketReader(refusal.Payload).ReadUInt()
            );

            // Nico points are not a currency this shop takes.
            session.Sent.Clear();
            await handler.HandleAsync(
                BuyBytes(listing.ScriptId, 100, 1),
                session,
                TestContext.Current.CancellationToken
            );
            Assert.NotEqual(0u, new PacketReader(Assert.Single(session.Sent).Payload).ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task DownloadRequest_OnlyForBuyersAndAuthors()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var (author, buyer, listing) = await SeedAsync(db);
            var shop = new AdventureShopRepository(db);
            var handler = new AreaAdventureShopDownloadRequestHandler(
                shop,
                NullLogger<AreaAdventureShopDownloadRequestHandler>.Instance
            );

            var stranger = new CapturingPlayerSession { UserId = buyer.Id, CharacterId = 2 };
            await handler.HandleAsync(
                ScriptIdBytes(listing.ScriptId),
                stranger,
                TestContext.Current.CancellationToken
            );
            var refused = new PacketReader(Assert.Single(stranger.Sent).Payload);
            Assert.Equal(
                AreaAdventureShopDownloadRequestHandler.NotEntitledResult,
                refused.ReadUInt()
            );
            Assert.Equal((ulong)listing.ScriptId, refused.ReadULong());

            var own = new CapturingPlayerSession { UserId = author.Id, CharacterId = 1 };
            await handler.HandleAsync(
                ScriptIdBytes(listing.ScriptId),
                own,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(53, Assert.Single(own.Sent).Payload.Length);
            Assert.Equal(0u, new PacketReader(own.Sent[0].Payload).ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UploadDelete_DelistsAndPushesWorkRegistry_ListsShrink()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var (author, buyer, listing) = await SeedAsync(db);
            var shop = new AdventureShopRepository(db);
            var works = new AdventureWorkRepository(db);
            var session = new CapturingPlayerSession { UserId = author.Id, CharacterId = 1 };

            await new AreaGetAdventureUploadListHandler(shop).HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );
            var uploads = Assert.Single(session.Sent);
            Assert.Equal(8 + AdventureUploadListRecord.WireSize, uploads.Payload.Length);
            var ur = new PacketReader(uploads.Payload);
            Assert.Equal(0u, ur.ReadUInt());
            Assert.Equal(1u, ur.ReadUInt());
            Assert.Equal((ulong)listing.ScriptId, ur.ReadULong());
            Assert.Equal("Yomogi", ur.ReadFixedString(AdventureShopItemRecord.AuthorNameLength));
            Assert.Equal("Thimbleglow", ur.ReadFixedString(AdventureShopItemRecord.TitleLength));
            Assert.Equal(100ul, ur.ReadULong());
            Assert.Equal(
                "A silly story",
                ur.ReadFixedString(AdventureUploadListRecord.CommentLength)
            );
            Assert.Equal((byte)0, ur.ReadByte());
            Assert.Equal(2u, ur.ReadUInt());
            Assert.Equal(20348ul, ur.ReadULong());

            Assert.Equal(
                AdventureBuyOutcome.Bought,
                (await shop.BuyAsync(buyer.Id, 2, listing.ScriptId, 100, 70)).Outcome
            );
            var buyerSession = new CapturingPlayerSession { UserId = buyer.Id, CharacterId = 2 };
            await new AreaGetAdventureDownloadListHandler(shop).HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                buyerSession,
                TestContext.Current.CancellationToken
            );
            var downloads = Assert.Single(buyerSession.Sent).Payload;
            Assert.Equal(8 + AdventureDownloadListRecord.WireSize, downloads.Length);
            // A sealed disc (公開しない) keeps the PC library's lock: trailing byte 0.
            Assert.Equal(0, downloads[^1]);

            // The 買取 clerk on the author's map gets re-announced so the window reloads both lists.
            db.Npcs.Add(
                new Npc
                {
                    MapId = 10030200,
                    ChannelId = -1,
                    NpcObjectId = 1342177331,
                    ModelId = 1002021,
                    Name = "はっぴぃ・すとぉりぃ買取",
                    InteractionType = NpcInteractionType.AdventureShopUpload,
                    IsEnabled = true,
                }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            session.MapId = 10030200;
            session.ChannelId = -1;
            session.Sent.Clear();
            await new AreaAdventureUploadDeleteRequestHandler(
                shop,
                new NpcRepository(db)
            ).HandleAsync(
                ScriptIdBytes(listing.ScriptId),
                session,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(
                [
                    PacketType.AdventureUploadDeleteRequestResponse,
                    PacketType.AdventureUploadStartedNotify,
                ],
                session.Sent.Select(p => p.Type)
            );
            var dr = new PacketReader(session.Sent[0].Payload);
            Assert.Equal(0u, dr.ReadUInt());
            Assert.Equal((ulong)listing.ScriptId, dr.ReadULong());
            Assert.Equal(1342177331u, new PacketReader(session.Sent[1].Payload).ReadUInt());
            var (_, worksAfter) = await works.GetWorksAsync(author.Id);
            Assert.False(Assert.Single(worksAfter).Uploaded);

            // Buyers keep their copy after the delisting; removing it from the download list hides it there only.
            buyerSession.Sent.Clear();
            await new AreaAdventureDownloadDeleteRequestHandler(shop).HandleAsync(
                ScriptIdBytes(listing.ScriptId),
                buyerSession,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(12, Assert.Single(buyerSession.Sent).Payload.Length);
            buyerSession.Sent.Clear();
            await new AreaGetAdventureDownloadListHandler(shop).HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                buyerSession,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(8, Assert.Single(buyerSession.Sent).Payload.Length);
            // Re-downloading from the history brings it back to the download list.
            var again = await shop.IssueDownloadTicketAsync(buyer.Id, listing.ScriptId);
            Assert.NotNull(again);
            Assert.NotNull(await shop.RedeemDownloadTicketAsync(again));
            Assert.Single(await shop.GetDownloadListAsync(buyer.Id));

            buyerSession.Sent.Clear();
            await new AreaAdventureShopRemoveAllBuyHistoryHandler(shop).HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                buyerSession,
                TestContext.Current.CancellationToken
            );
            Assert.Equal([0, 0, 0, 0], Assert.Single(buyerSession.Sent).Payload);
            Assert.Empty(await shop.GetHistoryAsync(buyer.Id, 50));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
