using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace aisp.Common.Services.Toxicity;

public readonly record struct LanguageIdResult(string Language, double Confidence);

/// <summary>
/// Compact ONNX port of Facebook fastText <c>lid.176</c>
/// (<see href="https://huggingface.co/TigreGotico/lid176-onnx">TigreGotico/lid176-onnx</see>,
/// CC-BY-SA-3.0). Feature hashing and hierarchical-softmax combination stay in managed
/// code because those steps have no portable ONNX ops.
/// </summary>
public sealed class FastTextLanguageId : IDisposable
{
    public const string HfRepo = "TigreGotico/lid176-onnx";
    public const string HfSha = "64f7d31b705bc4a7424c82762b323c6ee8c68b94";

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    readonly FastTextFeaturizer _featurizer;
    readonly HsCombiner _combiner;
    readonly string[] _labels;
    readonly string _modelPath;
    readonly Lock _lock = new();
    readonly ILogger? _logger;
    InferenceSession? _session;
    DateTime _lastUsedUtc = DateTime.UtcNow;
    bool _disposed;

    FastTextLanguageId(
        FastTextFeaturizer featurizer,
        HsCombiner combiner,
        string[] labels,
        string modelPath,
        ILogger? logger
    )
    {
        _featurizer = featurizer;
        _combiner = combiner;
        _labels = labels;
        _modelPath = modelPath;
        _logger = logger;
    }

    public static FastTextLanguageId? TryLoad(
        ToxicityClassifierOptions options,
        ILogger? logger = null
    )
    {
        if (
            string.IsNullOrWhiteSpace(options.LidModelPath)
            || string.IsNullOrWhiteSpace(options.LidVocabPath)
            || string.IsNullOrWhiteSpace(options.LidConfigPath)
            || string.IsNullOrWhiteSpace(options.LidLabelsPath)
            || string.IsNullOrWhiteSpace(options.LidHsTreePath)
        )
            return null;

        string[] paths =
        [
            options.LidModelPath,
            options.LidVocabPath,
            options.LidConfigPath,
            options.LidLabelsPath,
            options.LidHsTreePath,
        ];
        foreach (var path in paths)
        {
            if (!File.Exists(path))
                return null;
        }

        try
        {
            return Load(
                options.LidModelPath,
                options.LidVocabPath,
                options.LidConfigPath,
                options.LidLabelsPath,
                options.LidHsTreePath,
                logger
            );
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Language-id model failed to load; Latin text stays on RoBERTa");
            return null;
        }
    }

    public static FastTextLanguageId Load(
        string modelPath,
        string vocabPath,
        string configPath,
        string labelsPath,
        string hsTreePath,
        ILogger? logger = null
    )
    {
        var config =
            JsonSerializer.Deserialize<LidConfig>(File.ReadAllText(configPath), JsonOptions)
            ?? throw new InvalidDataException($"Invalid LID config: {configPath}");
        var words = ReadVocab(vocabPath);
        var labels =
            JsonSerializer.Deserialize<string[]>(File.ReadAllText(labelsPath), JsonOptions)
            ?? throw new InvalidDataException($"Invalid LID labels: {labelsPath}");
        var tree =
            JsonSerializer.Deserialize<HsTreeFile>(File.ReadAllText(hsTreePath), JsonOptions)
            ?? throw new InvalidDataException($"Invalid LID Huffman tree: {hsTreePath}");

        if (labels.Length == 0 || tree.Paths.Length != labels.Length)
            throw new InvalidDataException("LID labels and Huffman paths are mismatched.");

        return new FastTextLanguageId(
            new FastTextFeaturizer(words, config.Nwords, config.Minn, config.Maxn, config.Bucket),
            new HsCombiner(tree.Paths, tree.Codes),
            [.. labels.Select(StripLabel)],
            modelPath,
            logger
        );
    }

    public LanguageIdResult Detect(string text)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var ids = _featurizer.FeatureIds(text);
        if (ids.Length == 0)
            return new("und", 0);

        var nodeProbs = Infer(ids);
        var logProbs = _combiner.LogProbs(nodeProbs);
        var best = 0;
        for (int i = 1; i < logProbs.Length; i++)
        {
            if (logProbs[i] > logProbs[best])
                best = i;
        }

        return new(_labels[best], Math.Exp(logProbs[best]));
    }

    public void UnloadIdle(TimeSpan idle)
    {
        lock (_lock)
        {
            if (_session is null)
                return;
            if (
                !ToxicityClassifier.ShouldUnloadIdle(
                    _lastUsedUtc,
                    inFlight: 0,
                    idle,
                    DateTime.UtcNow
                )
            )
                return;

            _session.Dispose();
            _session = null;
            _logger?.LogInformation(
                "Unloaded language-id after {Minutes}m idle",
                (int)idle.TotalMinutes
            );
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;
            _session?.Dispose();
            _session = null;
            _disposed = true;
        }
    }

    float[] Infer(long[] ids)
    {
        var input = NamedOnnxValue.CreateFromTensor(
            "input_ids",
            new DenseTensor<long>(ids, [ids.Length])
        );
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_session is null)
            {
                _logger?.LogInformation("Loaded language-id from {Path}", _modelPath);
                using var options = new SessionOptions
                {
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                    IntraOpNumThreads = 1,
                    InterOpNumThreads = 1,
                    ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
                };
                _session = new InferenceSession(_modelPath, options);
            }

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run([
                input,
            ]);
            _lastUsedUtc = DateTime.UtcNow;
            return results[0].AsEnumerable<float>().ToArray();
        }
    }

    static string StripLabel(string label)
    {
        const string prefix = "__label__";
        return label.StartsWith(prefix, StringComparison.Ordinal) ? label[prefix.Length..] : label;
    }

    static string[] ReadVocab(string path)
    {
        var text = File.ReadAllText(path);
        var words = text.Split('\n');
        if (words.Length > 0 && words[^1].Length == 0)
            return words[..^1];
        return words;
    }

    sealed class LidConfig
    {
        public int Minn { get; set; } = 2;
        public int Maxn { get; set; } = 4;
        public int Bucket { get; set; } = 2_000_000;
        public int Nwords { get; set; }
    }

    sealed class HsTreeFile
    {
        public int[][] Paths { get; set; } = [];
        public bool[][] Codes { get; set; } = [];
    }
}

internal sealed class FastTextFeaturizer(
    IReadOnlyList<string> words,
    int nwords,
    int minn,
    int maxn,
    int bucket
)
{
    const string Eos = " ";
    const string Bow = "<";
    const string Eow = ">";

    readonly Dictionary<string, int> _word2Id = BuildWordMap(words);
    readonly Dictionary<int, int[]> _cache = [];

    public long[] FeatureIds(string text)
    {
        var line = new List<int>();
        foreach (var token in Tokenize(text))
            AddSubwords(line, token);
        var ids = new long[line.Count];
        for (int i = 0; i < line.Count; i++)
            ids[i] = line[i];
        return ids;
    }

    internal static string[] Tokenize(string text)
    {
        var normalized = text.Replace('\t', ' ')
            .Replace('\n', ' ')
            .Replace('\v', ' ')
            .Replace('\f', ' ')
            .Replace('\r', ' ');
        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var tokens = new string[parts.Length + 1];
        Array.Copy(parts, tokens, parts.Length);
        tokens[^1] = Eos;
        return tokens;
    }

    internal static uint Fnv1a(string value)
    {
        uint hash = 2166136261;
        foreach (byte b in Encoding.UTF8.GetBytes(value))
        {
            uint x = b;
            if (b >= 0x80)
                x |= 0xFFFFFF00;
            hash = (hash ^ x) * 16777619;
        }

        return hash;
    }

    internal static int[] ComputeSubwords(string word, int minn, int maxn, int bucket, int nwords)
    {
        var chars = word.EnumerateRunes().Select(rune => rune.ToString()).ToArray();
        var ids = new List<int>();
        int n = chars.Length;
        for (int i = 0; i < n; i++)
        {
            int maxJ = Math.Min(n, i + maxn);
            for (int j = i + minn; j <= maxJ; j++)
            {
                var ngram = string.Concat(chars[i..j]);
                ids.Add(nwords + (int)(Fnv1a(ngram) % (uint)bucket));
            }
        }

        return [.. ids];
    }

    void AddSubwords(List<int> line, string token)
    {
        if (!_word2Id.TryGetValue(token, out var wid))
        {
            if (token != Eos)
                line.AddRange(ComputeSubwords(Bow + token + Eow, minn, maxn, bucket, nwords));
            return;
        }

        if (maxn <= 0)
        {
            line.Add(wid);
            return;
        }

        line.AddRange(SubwordsOfKnown(wid, words[wid]));
    }

    int[] SubwordsOfKnown(int wid, string word)
    {
        if (_cache.TryGetValue(wid, out var cached))
            return cached;

        int[] ids;
        if (word == Eos)
            ids = [wid];
        else
        {
            var sub = ComputeSubwords(Bow + word + Eow, minn, maxn, bucket, nwords);
            ids = new int[1 + sub.Length];
            ids[0] = wid;
            sub.CopyTo(ids, 1);
        }

        _cache[wid] = ids;
        return ids;
    }

    static Dictionary<string, int> BuildWordMap(IReadOnlyList<string> words)
    {
        var map = new Dictionary<string, int>(words.Count, StringComparer.Ordinal);
        for (int i = 0; i < words.Count; i++)
            map[words[i]] = i;
        return map;
    }
}

internal sealed class HsCombiner(int[][] paths, bool[][] codes)
{
    public double[] LogProbs(ReadOnlySpan<float> nodeProbs)
    {
        var logF = new double[nodeProbs.Length];
        var log1mF = new double[nodeProbs.Length];
        for (int i = 0; i < nodeProbs.Length; i++)
        {
            double p = Math.Clamp(nodeProbs[i], 1e-12, 1.0);
            logF[i] = Math.Log(p);
            log1mF[i] = Math.Log(Math.Clamp(1.0 - nodeProbs[i], 1e-12, 1.0));
        }

        var logProbs = new double[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            var path = paths[i];
            var code = codes[i];
            int n = Math.Min(path.Length, code.Length);
            double sum = 0;
            for (int j = 0; j < n; j++)
            {
                int node = path[j];
                if ((uint)node >= (uint)nodeProbs.Length)
                    continue;
                sum += code[j] ? logF[node] : log1mF[node];
            }

            logProbs[i] = sum;
        }

        return logProbs;
    }
}

internal static class LanguageIdRouter
{
    public const int MinLetters = 12;
    public const double MinConfidence = 0.70;

    /// <summary>
    /// Languages DistilBERT was fine-tuned on (textdetox), excluding English.
    /// English overlaps with RoBERTa; RoBERTa wins because of multi-label heads.
    /// </summary>
    public static readonly HashSet<string> DistilBertLanguages = new(StringComparer.Ordinal)
    {
        "es",
        "fr",
        "de",
        "it",
        "ru",
        "uk",
        "zh",
        "yue",
        "wuu",
        "ar",
        "hi",
        "he",
        "ja",
        "tt",
        "am",
    };

    public readonly record struct ModelRoute(bool UseRoberta, string ModelName);

    public static ModelRoute Route(string normalizedText, LanguageIdResult? detection)
    {
        if (!ToxicityClassifier.IsLatinScript(normalizedText))
            return new(false, "DistilBERT");

        if (
            detection is { } hit
            && LetterCount(normalizedText) >= MinLetters
            && hit.Confidence >= MinConfidence
            && !IsRobertaLanguage(hit.Language)
        )
            return new(false, $"DistilBERT ({hit.Language})");

        return new(true, "RoBERTa (en)");
    }

    /// <summary>
    /// <c>unbiased-toxic-roberta</c> is Civil Comments English only. Prefer it for
    /// English; any other confident language uses DistilBERT (trained on that language,
    /// or multilingual encoder as the better untrained fallback).
    /// </summary>
    public static bool IsRobertaLanguage(string language) =>
        language.Equals("en", StringComparison.Ordinal);

    public static int LetterCount(string text)
    {
        int count = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.IsLetter(rune))
                count++;
        }

        return count;
    }
}
