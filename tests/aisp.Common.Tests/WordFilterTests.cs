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
            {
              "swears": ["fuck", "shit"],
              "slurs": ["faggot", "fagot", "nigger", "retard"]
            }
            """;

        var (blocked, slurs) = WordFilter.ParseTermLists(json);
        Assert.Equal(
            ["faggot", "fagot", "fuck", "nigger", "retard", "shit"],
            blocked.OrderBy(t => t).ToArray()
        );
        Assert.Equal(["faggot", "fagot", "nigger", "retard"], slurs.OrderBy(t => t).ToArray());
    }

    [Fact]
    public void ParseTermLists_UnionsSlursIntoCompleteList()
    {
        var (blocked, slurs) = WordFilter.ParseTermLists(
            """
            {
              "swears": ["fuck"],
              "slurs": ["faggot"]
            }
            """
        );

        Assert.Equal(["faggot", "fuck"], blocked.OrderBy(t => t).ToArray());
        Assert.Equal(["faggot"], slurs.ToArray());
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
    public void LoadsTermsFromJsonFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"blocked-words-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(
                path,
                """
                {
                  "swears": ["blow job"],
                  "slurs": ["faggot", "fag"]
                }
                """
            );
            var filter = new WordFilter(path, logger: null);
            Assert.True(filter.ContainsBlockedWord(WordFilterLevel.Complete, "fag"));
            Assert.True(filter.ContainsBlockedWord(WordFilterLevel.Complete, "blow-job"));
            Assert.True(filter.ContainsBlockedWord(WordFilterLevel.NoSlurs, "Faggot"));
            Assert.False(filter.ContainsBlockedWord(WordFilterLevel.NoSlurs, "blowjob"));
            Assert.False(filter.ContainsBlockedWord(WordFilterLevel.Complete, "clean"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MissingFile_DoesNotThrowAndBlocksNothing()
    {
        var filter = new WordFilter(
            Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json"),
            logger: null
        );
        Assert.False(filter.ContainsBlockedWord(WordFilterLevel.Complete, "faggot"));
    }

    [Fact]
    public void SeedList_BlocksSlursInChatAndSwearsInNames()
    {
        Assert.True(File.Exists(WordFilter.DefaultListPath));
        var filter = new WordFilter(WordFilter.DefaultListPath, logger: null);

        Assert.True(filter.ContainsBlockedWord(WordFilterLevel.Complete, "fuck"));
        Assert.True(filter.ContainsBlockedWord(WordFilterLevel.Complete, "shit"));
        Assert.False(filter.ContainsBlockedWord(WordFilterLevel.NoSlurs, "fuck"));
        Assert.False(filter.ContainsBlockedWord(WordFilterLevel.NoSlurs, "holy shit"));
        Assert.True(filter.ContainsBlockedWord(WordFilterLevel.NoSlurs, "Faggot"));
        Assert.True(filter.ContainsBlockedWord(WordFilterLevel.NoSlurs, "nigger"));
        Assert.True(filter.ContainsBlockedWord(WordFilterLevel.NoSlurs, "retard"));
        Assert.False(filter.ContainsBlockedWord(WordFilterLevel.Complete, "enby"));
        Assert.False(filter.ContainsBlockedWord(WordFilterLevel.Complete, "twink"));
    }
}
