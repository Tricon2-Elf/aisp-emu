using aisp.Common.Services.Toxicity;

namespace aisp.Common.Tests;

public sealed class LanguageIdRouterTests
{
    const string Spanish = "esto es un mensaje bastante largo";

    [Fact]
    public void Fnv1a_MatchesFastTextSignedByteHash()
    {
        Assert.Equal(1294271946u, FastTextFeaturizer.Fnv1a("ab"));
        Assert.Equal(2916489882u, FastTextFeaturizer.Fnv1a("<hi>"));
        Assert.Equal(620380921u, FastTextFeaturizer.Fnv1a("ñ"));
        Assert.Equal(1176137065u, FastTextFeaturizer.Fnv1a("es"));
        Assert.Equal(2356916205u, FastTextFeaturizer.Fnv1a("<hola>"));
    }

    [Fact]
    public void ComputeSubwords_HashesCharNgramsIntoBuckets()
    {
        int[] ids = FastTextFeaturizer.ComputeSubwords("<hola>", 2, 4, 2_000_000, 40010);
        Assert.Equal(
            [
                1605427,
                562876,
                1177220,
                68978,
                1612502,
                1381473,
                405312,
                1679871,
                927515,
                1262504,
                530538,
                1654816,
            ],
            ids
        );
    }

    [Fact]
    public void Tokenize_SplitsOnWhitespaceAndAppendsEos()
    {
        Assert.Equal(["Hello", "world", " "], FastTextFeaturizer.Tokenize("Hello world"));
        Assert.Equal(["Hello", "world", " "], FastTextFeaturizer.Tokenize("Hello\tworld\n"));
    }

    [Fact]
    public void HsCombiner_WalksHuffmanPathsInLogSpace()
    {
        var combiner = new HsCombiner(
            [
                [0, 1],
                [0],
            ],
            [
                [true, false],
                [false],
            ]
        );
        var log = combiner.LogProbs([0.8f, 0.3f]);
        Assert.Equal(Math.Log(0.8f) + Math.Log(1.0 - 0.3f), log[0], 1e-9);
        Assert.Equal(Math.Log(1.0 - 0.8f), log[1], 1e-9);
    }

    [Fact]
    public void Route_NonLatinGoesToDistilBert()
    {
        var route = LanguageIdRouter.Route("これは日本語です", detection: null);
        Assert.False(route.UseRoberta);
        Assert.Equal("DistilBERT", route.ModelName);
    }

    [Fact]
    public void Route_ConfidentSpanishGoesToDistilBert()
    {
        var route = LanguageIdRouter.Route(Spanish, new LanguageIdResult("es", 0.95));
        Assert.False(route.UseRoberta);
        Assert.Equal("DistilBERT (es)", route.ModelName);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("it")]
    [InlineData("pt")]
    [InlineData("nl")]
    public void Route_ConfidentNonEnglishGoesToDistilBert(string language)
    {
        var route = LanguageIdRouter.Route(Spanish, new LanguageIdResult(language, 0.90));
        Assert.False(route.UseRoberta);
        Assert.Equal($"DistilBERT ({language})", route.ModelName);
    }

    [Fact]
    public void Route_ShortSpanishStaysOnRoberta()
    {
        var route = LanguageIdRouter.Route("hola amigos", new LanguageIdResult("es", 0.99));
        Assert.True(route.UseRoberta);
        Assert.Equal("RoBERTa (en)", route.ModelName);
    }

    [Fact]
    public void Route_LowConfidenceSpanishStaysOnRoberta()
    {
        var route = LanguageIdRouter.Route(Spanish, new LanguageIdResult("es", 0.50));
        Assert.True(route.UseRoberta);
    }

    [Fact]
    public void Route_EnglishAndUncertainLatinStayOnRoberta()
    {
        var english = LanguageIdRouter.Route(Spanish, new LanguageIdResult("en", 0.99));
        Assert.True(english.UseRoberta);
        Assert.Equal("RoBERTa (en)", english.ModelName);
        Assert.True(LanguageIdRouter.Route(Spanish, detection: null).UseRoberta);
        Assert.DoesNotContain("en", LanguageIdRouter.DistilBertLanguages);
        Assert.True(LanguageIdRouter.IsRobertaLanguage("en"));
    }

    [Fact]
    public void LetterCount_IgnoresNonLetters()
    {
        Assert.Equal(10, LanguageIdRouter.LetterCount("hola amigos!"));
        Assert.True(LanguageIdRouter.LetterCount(Spanish) >= LanguageIdRouter.MinLetters);
    }

    [Fact]
    public void TryLoad_ReturnsNullWhenLidPathsMissing()
    {
        var options = new ToxicityClassifierOptions("a.onnx", "a.json", "b.onnx", "b.json");
        Assert.Null(FastTextLanguageId.TryLoad(options));
    }

    [Fact]
    public void FromModelRoot_IncludesLid176Layout()
    {
        var options = ToxicityClassifierOptions.FromModelRoot("/data/models");
        Assert.Equal(
            Path.Combine("/data/models", "lid176", "onnx", "lid176.int8.onnx"),
            options.LidModelPath
        );
        Assert.Equal(Path.Combine("/data/models", "lid176", "vocab.txt"), options.LidVocabPath);
        Assert.Equal(Path.Combine("/data/models", "lid176", "config.json"), options.LidConfigPath);
        Assert.Equal(Path.Combine("/data/models", "lid176", "labels.json"), options.LidLabelsPath);
        Assert.Equal(Path.Combine("/data/models", "lid176", "hs_tree.json"), options.LidHsTreePath);
    }
}
