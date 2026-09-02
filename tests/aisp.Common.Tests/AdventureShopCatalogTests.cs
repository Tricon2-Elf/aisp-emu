using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Tests;

public sealed class AdventureShopCatalogTests
{
    private static async Task<(User Author, User Buyer, AdventureListing Listing)> SeedAsync(
        MainContext db
    )
    {
        var author = new User { Username = "author" };
        author.SetPassword("pw");
        var buyer = new User { Username = "buyer", AiPoints = 5000 };
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

    private static void AssertRecord(ref PacketReader reader, AdventureListing listing, uint sales)
    {
        Assert.Equal((ulong)listing.ScriptId, reader.ReadULong());
        Assert.Equal("Yomogi", reader.ReadFixedString(AdventureShopItemRecord.AuthorNameLength));
        Assert.Equal("Thimbleglow", reader.ReadFixedString(AdventureShopItemRecord.TitleLength));
        Assert.Equal(100ul, reader.ReadULong());
        Assert.Equal(0ul, reader.ReadULong());
        Assert.Equal("学園もの", reader.ReadFixedString(AdventureShopItemRecord.TagLength));
        for (var i = 1; i < AdventureShopItemRecord.TagCount; i++)
            Assert.Equal("", reader.ReadFixedString(AdventureShopItemRecord.TagLength));
        Assert.Equal((ushort)1, reader.ReadUShort());
        Assert.Equal((byte)0, reader.ReadByte());
        Assert.Equal(
            "A silly story",
            reader.ReadFixedString(AdventureShopItemRecord.CommentLength)
        );
        Assert.Equal((byte)0, reader.ReadByte());
        reader.ReadByte();
        Assert.InRange(reader.ReadUInt(), 1u, uint.MaxValue);
        reader.ReadUInt();
        Assert.Equal(sales, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(20348ul, reader.ReadULong());
    }

    [Fact]
    public async Task Snapshot_CarriesLineupRankingAndHistory()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var (_, buyer, listing) = await SeedAsync(db);
            var shop = new AdventureShopRepository(db);
            Assert.Equal(
                AdventureBuyOutcome.Bought,
                (await shop.BuyAsync(buyer.Id, 2, listing.ScriptId, 100, 70)).Outcome
            );

            var snapshot = await new AdventureShopCatalog(shop).BuildSnapshotAsync(buyer.Id);
            var bytes = snapshot.ToBytes();
            // 45-byte header/counters + one item + one ranking row + one history row.
            Assert.Equal(45 + 1589 + 1595 + 1594, bytes.Length);
            var reader = new PacketReader(bytes);
            Assert.Equal(1ul, reader.ReadULong());
            Assert.Equal("", reader.ReadString());
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(1ul, reader.ReadULong());
            Assert.Equal(1u, reader.ReadUInt());
            AssertRecord(ref reader, listing, 1);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(1u, reader.ReadUInt());
            AssertRecord(ref reader, listing, 1);
            Assert.Equal((ushort)1, reader.ReadUShort());
            Assert.Equal(1u, reader.ReadUInt());
            Assert.Equal(1u, reader.ReadUInt());
            AssertRecord(ref reader, listing, 1);
            reader.ReadByte();
            Assert.InRange(reader.ReadUInt(), 1u, uint.MaxValue);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GenreSearch_SendsPageThenResult()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var (_, buyer, listing) = await SeedAsync(db);
            var handler = new AreaAdventureShopGenreSearchHandler(
                new AdventureShopCatalog(new AdventureShopRepository(db))
            );
            var session = new CapturingPlayerSession { UserId = buyer.Id, CharacterId = 2 };

            var writer = new PacketWriter();
            writer.Write(2u);
            writer.Write(0u);
            writer.Write(1u);
            writer.Write(0u);
            await handler.HandleAsync(
                writer.ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(2, session.Sent.Count);
            Assert.Equal(PacketType.AdventureShopItemNotify, session.Sent[0].Type);
            var reader = new PacketReader(session.Sent[0].Payload);
            Assert.Equal("", reader.ReadString());
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(1u, reader.ReadUInt());
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(1ul, reader.ReadULong());
            Assert.Equal(1u, reader.ReadUInt());
            AssertRecord(ref reader, listing, 0);
            Assert.Equal(PacketType.AdventureShopGenreSearchResponse, session.Sent[1].Type);
            Assert.Equal([0, 0, 0, 0], session.Sent[1].Payload);

            // 総合 (tab 0) lists every genre.
            session.Sent.Clear();
            var all = new PacketWriter();
            all.Write(0u);
            all.Write(0u);
            all.Write(0u);
            all.Write(0u);
            await handler.HandleAsync(
                all.ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );
            var allReader = new PacketReader(session.Sent[0].Payload);
            allReader.ReadString();
            allReader.ReadUInt();
            allReader.ReadUInt();
            allReader.ReadUInt();
            Assert.Equal(1ul, allReader.ReadULong());

            // Another genre tab: an empty page, still followed by the release.
            session.Sent.Clear();
            var other = new PacketWriter();
            other.Write(5u);
            other.Write(0u);
            other.Write(0u);
            other.Write(0u);
            await handler.HandleAsync(
                other.ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(1 + 4 + 4 + 4 + 8 + 4, session.Sent[0].Payload.Length);
            Assert.Equal(PacketType.AdventureShopGenreSearchResponse, session.Sent[1].Type);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
