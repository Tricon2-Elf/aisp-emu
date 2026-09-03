using aisp.Common.Game;

namespace aisp.Common.Tests;

public class WordFilterTests
{
    [Theory]
    [InlineData("Faggot")]
    [InlineData("fAgGoT")]
    [InlineData("x.faggot.x")]
    [InlineData("my faggot doll")]
    [InlineData("F-A-G-G-O-T")]
    [InlineData("f4gg0t")]
    [InlineData("F4G")]
    [InlineData("f@g")]
    public void ContainsBlockedWord_DetectsNormalizedSubstrings(string name)
    {
        var filter = WordFilter.FromTerms(["faggot", "fag"]);
        Assert.True(filter.ContainsBlockedWord(WordFilterLevel.Complete, name));
    }

    [Theory]
    [InlineData("Robot")]
    [InlineData("Alice")]
    [InlineData("")]
    [InlineData("   ")]
    public void ContainsBlockedWord_AllowsCleanNames(string name)
    {
        var filter = WordFilter.FromTerms(["faggot", "fag"]);
        Assert.False(filter.ContainsBlockedWord(WordFilterLevel.Complete, name));
    }

    [Fact]
    public void ContainsBlockedWord_EmptyListNeverBlocks()
    {
        var filter = WordFilter.FromTerms(Array.Empty<string>());
        Assert.False(filter.ContainsBlockedWord(WordFilterLevel.Complete, "faggot"));
    }

    [Fact]
    public void ContainsBlockedWord_DetectsBlockedFieldAmongCleanOnes()
    {
        var filter = WordFilter.FromTerms(["faggot"]);
        Assert.True(filter.ContainsBlockedWord(WordFilterLevel.Complete, "tea", "Faggot", "maps"));
        Assert.False(filter.ContainsBlockedWord(WordFilterLevel.Complete, "tea", "maps", "robots"));
        Assert.False(filter.ContainsBlockedWord(WordFilterLevel.Complete));
    }

    [Fact]
    public void ContainsBlockedWord_AllowedNeverBlocks()
    {
        var filter = WordFilter.FromTerms(["faggot"], ["faggot"]);
        Assert.False(filter.ContainsBlockedWord(WordFilterLevel.Allowed, "faggot"));
        Assert.False(filter.ContainsBlockedWord(WordFilterLevel.Allowed, "fuck"));
    }

    [Fact]
    public void ContainsBlockedWord_NoSlursIgnoresSwears()
    {
        var filter = WordFilter.FromTerms(["fuck", "shit", "faggot"], ["faggot"]);
        Assert.True(filter.ContainsBlockedWord(WordFilterLevel.Complete, "fuck"));
        Assert.True(filter.ContainsBlockedWord(WordFilterLevel.Complete, "shit"));
        Assert.False(filter.ContainsBlockedWord(WordFilterLevel.NoSlurs, "fuck"));
        Assert.False(filter.ContainsBlockedWord(WordFilterLevel.NoSlurs, "holy shit"));
        Assert.True(filter.ContainsBlockedWord(WordFilterLevel.NoSlurs, "Faggot"));
    }

    [Fact]
    public void ParseTermLists_JsonSplitsSlursFromSwears()
    {
        var json = """
            [
              {"id":"fuck","match":"fuck","tags":["general"]},
              {"id":"faggot","match":"faggot|fagot","tags":["lgbtq"]},
              {"id":"nigger","match":"nigger","tags":["racial"]},
              {"id":"retard","match":"retard|retarded","tags":["general"]},
              {"id":"enby","match":"enby","tags":["lgbtq"]}
            ]
            """;

        var policy = WordFilter.ParsePolicy(
            """
            {
              "chat": {
                "slurTags": ["racial", "lgbtq"],
                "extraSlurIds": ["retard", "raghead"],
                "allowedIds": ["enby"]
              }
            }
            """
        );
        var (blocked, slurs) = WordFilter.ParseTermLists(json, policy);
        Assert.Equal(
            ["enby", "faggot", "fagot", "fuck", "nigger", "retard", "retarded"],
            blocked.OrderBy(t => t).ToArray()
        );
        Assert.Equal(
            ["faggot", "fagot", "nigger", "retard", "retarded"],
            slurs.OrderBy(t => t).ToArray()
        );
        Assert.True(WordFilter.IsChatSlurEntry("raghead", ["religious"], policy));
        Assert.False(WordFilter.IsChatSlurEntry("enby", ["lgbtq"], policy));
        Assert.False(WordFilter.IsChatSlurEntry("fuck", ["general"], policy));
    }

    [Fact]
    public void PolicyFile_DefinesChatSlurTagsAndAllowedIds()
    {
        Assert.True(File.Exists(WordFilter.DefaultPolicyPath));
        var policy = WordFilter.LoadPolicy(WordFilter.DefaultPolicyPath, logger: null);
        Assert.Contains("racial", policy.ChatSlurTags);
        Assert.Contains("lgbtq", policy.ChatSlurTags);
        Assert.Contains("retard", policy.ChatExtraSlurIds);
        Assert.Contains("enby", policy.ChatAllowedIds);
        Assert.DoesNotContain("fuck", policy.ChatExtraSlurIds);
    }

    [Fact]
    public void ParseTermLists_PlainTextUsesTheSameListForChat()
    {
        var (blocked, slurs) = WordFilter.ParseTermLists("fuck\nfaggot\n");
        Assert.Equal(blocked, slurs);
        Assert.Equal(["faggot", "fuck"], blocked.OrderBy(t => t).ToArray());
    }

    [Fact]
    public void Normalize_StripsSeparatorsLowercasesAndMapsLeet()
    {
        Assert.Equal("faggot", WordFilter.Normalize("F.a G-g_Ot"));
        Assert.Equal("fag", WordFilter.Normalize("f4g"));
        Assert.Equal("faggot", WordFilter.Normalize("f4gg0t"));
        Assert.Equal("ass", WordFilter.Normalize("@$$"));
        Assert.Equal("shit", WordFilter.Normalize("$h1t"));
    }

    [Fact]
    public void ParseTerms_ReadsOneEntryPerLine()
    {
        var terms = WordFilter.ParseTerms(
            """
            faggot
            # comment
            blow job

            """
        );

        Assert.Equal(["blowjob", "faggot"], terms.OrderBy(t => t).ToArray());
    }

    [Fact]
    public void LoadsTermsFromCachedTxtFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"blocked-words-{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllText(path, "faggot\nfag\n");
            var filter = new WordFilter(path, logger: null, fetchRemote: null);
            Assert.True(filter.ContainsBlockedWord(WordFilterLevel.Complete, "fag"));
            Assert.False(filter.ContainsBlockedWord(WordFilterLevel.Complete, "clean"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MissingFile_WithoutRemote_DoesNotThrowAndBlocksNothing()
    {
        var filter = new WordFilter(
            Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.txt"),
            logger: null,
            fetchRemote: null
        );
        Assert.False(filter.ContainsBlockedWord(WordFilterLevel.Complete, "faggot"));
    }

    [Fact]
    public void MissingFile_DownloadsAndCachesLocally()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"blocked-words-cache-{Guid.NewGuid():N}");
        var path = Path.Combine(dir, "blockedWords.txt");
        try
        {
            var filter = new WordFilter(
                path,
                logger: null,
                fetchRemote: (_, _) => "faggot\nblow job\n"
            );

            Assert.True(File.Exists(path));
            Assert.True(filter.ContainsBlockedWord(WordFilterLevel.Complete, "Faggot"));
            Assert.True(filter.ContainsBlockedWord(WordFilterLevel.Complete, "blow-job"));
            Assert.True(filter.ContainsBlockedWord(WordFilterLevel.NoSlurs, "Faggot"));

            // Second load uses cache; fetcher must not be required.
            var cached = new WordFilter(path, logger: null, fetchRemote: null);
            Assert.True(cached.ContainsBlockedWord(WordFilterLevel.Complete, "faggot"));
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void JsonCache_ChatAllowsSwearsAndBlocksSlurs()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"blocked-words-json-{Guid.NewGuid():N}");
        var path = Path.Combine(dir, "blockedWords.json");
        try
        {
            var filter = new WordFilter(
                path,
                logger: null,
                fetchRemote: (_, _) =>
                    """
                    [
                      {"id":"fuck","match":"fuck","tags":["general"]},
                      {"id":"faggot","match":"faggot","tags":["lgbtq"]},
                      {"id":"nigger","match":"nigger","tags":["racial"]}
                    ]
                    """
            );

            Assert.True(File.Exists(path));
            Assert.True(filter.ContainsBlockedWord(WordFilterLevel.Complete, "fuck"));
            Assert.False(filter.ContainsBlockedWord(WordFilterLevel.NoSlurs, "fuck"));
            Assert.True(filter.ContainsBlockedWord(WordFilterLevel.NoSlurs, "Faggot"));
            Assert.True(filter.ContainsBlockedWord(WordFilterLevel.NoSlurs, "nigger"));
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }
}
