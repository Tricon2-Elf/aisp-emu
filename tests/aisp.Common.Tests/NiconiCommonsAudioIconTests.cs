using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class NiconiCommonsAudioIconTests
{
    [Fact]
    public async Task Base_lists_send_shared_audio_icons_without_changing_registry_ids()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var connectionLifetime = connection;
        await using var db = new MainContext(options);
        await DramaTestCatalog.SeedAsync(db);
        var catalog = new DramaCatalog(db, TestTextLocaliser.English);
        var characters = new CharacterRepository(db, NullLogger<CharacterRepository>.Instance);
        var session = new CapturingPlayerSession();
        var ct = TestContext.Current.CancellationToken;
        await new AreaNiconiCommonsBaseListHandler(characters, catalog).HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            ct
        );
        var commons = Assert.Single(session.Sent).Payload;
        var reader = new PacketReader(commons);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(13u, reader.ReadUInt()); // three licensed titles, nine BGM, one SE
        for (var i = 0; i < 10; i++)
        {
            var row = new PacketReader(commons.AsSpan(8 + (3 + i) * 113));
            Assert.Equal(i < 9 ? 32001001u + (uint)i : 42001001u, row.ReadUInt());
            Assert.Equal(i < 9 ? 14300002u : 14300003u, row.ReadUInt());
            Assert.Equal(i < 9 ? 2u : 3u, row.ReadUInt());
        }

        session.Sent.Clear();
        await new AreaUccVoiceBaseListHandler(characters, catalog).HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            ct
        );
        var voices = Assert.Single(session.Sent).Payload;
        for (var i = 0; i < 3; i++)
        {
            var row = new PacketReader(voices.AsSpan(8 + i * 874));
            Assert.Equal(10001u + (uint)i, row.ReadUInt());
            Assert.Equal(14300001u, row.ReadUInt());
        }
    }
}
