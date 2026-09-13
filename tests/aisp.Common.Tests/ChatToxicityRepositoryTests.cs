using aisp.Common.Config;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Services.Toxicity;
using aisp.Common.Tests.Support;

namespace aisp.Common.Tests;

public sealed class ChatToxicityRepositoryTests
{
    sealed class CapturingClassifier : IChatToxicityClassifier
    {
        public List<(long Id, string Message)> Enqueued { get; } = [];
        public bool Enabled { get; set; } = true;

        public bool TryEnqueue(long id, string message)
        {
            if (!Enabled)
                return false;
            Enqueued.Add((id, message));
            return true;
        }
    }

    [Fact]
    public async Task AddAsync_EnqueuesWhenClassifierPresent()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await using var db = new MainContext(options);
        var classifier = new CapturingClassifier();
        var repo = new ChatLogRepository(db, classifier);

        await repo.AddAsync(
            new ChatMessage
            {
                Kind = ChatLogKind.Public,
                UserId = 1,
                CharacterId = 1,
                CharacterName = "a",
                Message = "hello",
            },
            TestContext.Current.CancellationToken
        );

        var item = Assert.Single(classifier.Enqueued);
        Assert.Equal("hello", item.Message);
        Assert.True(item.Id > 0);
    }

    [Fact]
    public async Task AddAsync_SkipsEnqueueWhenClassifierNull()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await using var db = new MainContext(options);
        var repo = new ChatLogRepository(db);

        await repo.AddAsync(
            new ChatMessage
            {
                Kind = ChatLogKind.Public,
                UserId = 1,
                CharacterId = 1,
                CharacterName = "a",
                Message = "hello",
            },
            TestContext.Current.CancellationToken
        );
    }

    [Fact]
    public async Task SetToxicityAsync_UpdatesRow()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await using var db = new MainContext(options);
        var repo = new ChatLogRepository(db);

        await repo.AddAsync(
            new ChatMessage
            {
                Kind = ChatLogKind.Public,
                UserId = 1,
                CharacterId = 1,
                CharacterName = "a",
                Message = "hello",
            },
            TestContext.Current.CancellationToken
        );

        var id = db.ChatMessages.Single().Id;
        await repo.SetToxicityAsync(
            id,
            toxicity: true,
            reason: "insult 81%; threat 8%",
            TestContext.Current.CancellationToken
        );

        db.ChangeTracker.Clear();
        var row = db.ChatMessages.Single(x => x.Id == id);
        Assert.True(row.Toxicity);
        Assert.Equal("insult 81%; threat 8%", row.ToxicityReason);
    }

    [Fact]
    public async Task ListUnclassifiedAsync_ReturnsNewestEmptyReasonRows()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await using var db = new MainContext(options);
        var repo = new ChatLogRepository(db);

        await repo.AddAsync(
            new ChatMessage
            {
                Kind = ChatLogKind.Public,
                UserId = 1,
                CharacterId = 1,
                CharacterName = "a",
                Message = "old",
            },
            TestContext.Current.CancellationToken
        );
        await repo.AddAsync(
            new ChatMessage
            {
                Kind = ChatLogKind.Public,
                UserId = 1,
                CharacterId = 1,
                CharacterName = "a",
                Message = "classified",
            },
            TestContext.Current.CancellationToken
        );
        await repo.AddAsync(
            new ChatMessage
            {
                Kind = ChatLogKind.Public,
                UserId = 1,
                CharacterId = 1,
                CharacterName = "a",
                Message = "new",
            },
            TestContext.Current.CancellationToken
        );

        var classified = db.ChatMessages.Single(x => x.Message == "classified");
        await repo.SetToxicityAsync(
            classified.Id,
            false,
            "toxicity 1%; severe_toxicity 0%; obscene 0%; identity_attack 0%; insult 0%; threat 0%; sexual_explicit 0%",
            TestContext.Current.CancellationToken
        );

        var rows = await repo.ListUnclassifiedAsync(10, TestContext.Current.CancellationToken);
        Assert.Equal(["new", "old"], rows.Select(x => x.Message).ToArray());
    }

    [Fact]
    public void ResolveModelRoot_UsesDbDirectoryWhenModelRootEmpty()
    {
        var opts = new ChatToxicityOptions { ModelRoot = "" };
        var db = new DbOptions { ConnectionString = "Data Source=/data/main.db" };
        var root = ChatToxicityPaths.ResolveModelRoot(opts, db);
        Assert.Equal(Path.Combine("/data", "models"), root);
    }

    [Fact]
    public void ResolveModelRoot_UsesExplicitModelRoot()
    {
        var opts = new ChatToxicityOptions { ModelRoot = "/custom/models" };
        var db = new DbOptions { ConnectionString = "Data Source=/data/main.db" };
        Assert.Equal(
            Path.GetFullPath("/custom/models"),
            ChatToxicityPaths.ResolveModelRoot(opts, db)
        );
    }
}
