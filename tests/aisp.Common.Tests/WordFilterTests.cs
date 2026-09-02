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
        Assert.True(filter.ContainsBlockedWord(name));
    }

    [Theory]
    [InlineData("Robot")]
    [InlineData("Alice")]
    [InlineData("")]
    [InlineData("   ")]
    public void ContainsBlockedWord_AllowsCleanNames(string name)
    {
        var filter = WordFilter.FromTerms(["faggot", "fag"]);
        Assert.False(filter.ContainsBlockedWord(name));
    }

    [Fact]
    public void ContainsBlockedWord_EmptyListNeverBlocks()
    {
        var filter = WordFilter.FromTerms(Array.Empty<string>());
        Assert.False(filter.ContainsBlockedWord("faggot"));
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
            Assert.True(filter.ContainsBlockedWord("fag"));
            Assert.False(filter.ContainsBlockedWord("clean"));
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
        Assert.False(filter.ContainsBlockedWord("faggot"));
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
            Assert.True(filter.ContainsBlockedWord("Faggot"));
            Assert.True(filter.ContainsBlockedWord("blow-job"));

            // Second load uses cache; fetcher must not be required.
            var cached = new WordFilter(path, logger: null, fetchRemote: null);
            Assert.True(cached.ContainsBlockedWord("faggot"));
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }
}
