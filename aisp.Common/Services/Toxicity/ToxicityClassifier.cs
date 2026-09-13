using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Tokenizers.DotNet;

namespace aisp.Common.Services.Toxicity;

/// <summary>
/// Dual-model toxicity classifier. Sessions are created on first use and can be
/// unloaded after idle via <see cref="UnloadIdle"/>.
/// </summary>
public sealed partial class ToxicityClassifier : IDisposable
{
    const int SevereToxicity = 1;
    const int IdentityAttack = 3;
    const int Insult = 4;
    const int Threat = 5;

    static readonly string[] RobertaLabels =
    [
        "toxicity",
        "severe_toxicity",
        "obscene",
        "identity_attack",
        "insult",
        "threat",
        "sexual_explicit",
    ];

    readonly ToxicityClassifierOptions _options;
    readonly ILogger? _logger;
    readonly LazyModelSlot _roberta;
    readonly LazyModelSlot _distilBert;
    bool _disposed;

    public ToxicityClassifier(ToxicityClassifierOptions options, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        Validate(options);

        _options = options;
        _logger = logger;
        _roberta = new LazyModelSlot(
            "RoBERTa",
            options.RobertaModelPath,
            options.RobertaTokenizerPath,
            padId: 1,
            options.MaxLength,
            options.MaxConcurrency,
            logger
        );
        _distilBert = new LazyModelSlot(
            "DistilBERT",
            options.DistilBertModelPath,
            options.DistilBertTokenizerPath,
            padId: 0,
            options.MaxLength,
            options.MaxConcurrency,
            logger
        );
    }

    public static ToxicityClassifier Load(string modelRoot = "models", ILogger? logger = null) =>
        new(ToxicityClassifierOptions.FromModelRoot(modelRoot), logger);

    public ToxicityResult Classify(string text)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        string normalizedText = Normalize(text);
        bool useRoberta = IsLatinScript(normalizedText);
        string model = useRoberta ? "RoBERTa (en)" : "DistilBERT";
        ScoreResult original = Score(text, useRoberta);
        ScoreResult? normalized = normalizedText == text ? null : Score(normalizedText, useRoberta);

        return new(model, original, normalized);
    }

    public ToxicityResult[] ClassifyMany(IReadOnlyList<string> messages)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(messages);

        if (messages.Count == 0)
            return [];

        var originals = new ScoreResult?[messages.Count];
        var normalized = new ScoreResult?[messages.Count];
        var useRoberta = new bool[messages.Count];
        var robertaWork = new List<WorkItem>(messages.Count);
        var distilBertWork = new List<WorkItem>(messages.Count);

        for (int i = 0; i < messages.Count; i++)
        {
            string text = messages[i];
            ArgumentException.ThrowIfNullOrWhiteSpace(text);

            string normalizedText = Normalize(text);
            useRoberta[i] = IsLatinScript(normalizedText);
            List<WorkItem> work = useRoberta[i] ? robertaWork : distilBertWork;
            work.Add(new(i, text, IsNormalized: false));

            if (normalizedText != text)
                work.Add(new(i, normalizedText, IsNormalized: true));
        }

        ScoreRobertaBatches(robertaWork, originals, normalized);
        ScoreDistilBertBatches(distilBertWork, originals, normalized);

        var results = new ToxicityResult[messages.Count];
        for (int i = 0; i < results.Length; i++)
        {
            results[i] = new(
                useRoberta[i] ? "RoBERTa (en)" : "DistilBERT",
                originals[i]!,
                normalized[i]
            );
        }

        return results;
    }

    /// <summary>
    /// Disposes any loaded session whose last use is older than <paramref name="idle"/>.
    /// </summary>
    public void UnloadIdle(TimeSpan idle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (idle <= TimeSpan.Zero)
            return;

        _roberta.UnloadIfIdle(idle);
        _distilBert.UnloadIfIdle(idle);
    }

    /// <summary>Test/diagnostics: whether the RoBERTa session is currently loaded.</summary>
    public bool IsRobertaLoaded => _roberta.IsLoaded;

    /// <summary>Test/diagnostics: whether the DistilBERT session is currently loaded.</summary>
    public bool IsDistilBertLoaded => _distilBert.IsLoaded;

    ScoreResult Score(string text, bool useRoberta) =>
        useRoberta ? ScoreRoberta(text) : ScoreDistilBert(text);

    ScoreResult ScoreRoberta(string text) => ScoreRoberta(text, _roberta.Infer(text));

    ScoreResult ScoreRoberta(string text, ReadOnlySpan<float> logits)
    {
        var scores = new LabelScore[RobertaLabels.Length];
        bool swearOnly = IsSwearOnly(text);
        bool toxic = false;

        for (int i = 0; i < scores.Length; i++)
        {
            double score = Sigmoid(logits[i]);
            bool counts = i is SevereToxicity or IdentityAttack or Insult or Threat;

            if (i == Insult && swearOnly)
                counts = false;

            scores[i] = new(RobertaLabels[i], score, counts);
            double threshold = i == Insult ? _options.InsultThreshold : _options.RobertaThreshold;
            toxic |= counts && score >= threshold;
        }

        return new(text, toxic, scores);
    }

    ScoreResult ScoreDistilBert(string text) => ScoreDistilBert(text, _distilBert.Infer(text));

    ScoreResult ScoreDistilBert(string text, ReadOnlySpan<float> logits)
    {
        double toxic = SoftmaxClassOne(logits);

        return new(
            text,
            toxic >= _options.DistilBertThreshold,
            [new("toxic", toxic, true), new("not-toxic", 1 - toxic, false)]
        );
    }

    void ScoreRobertaBatches(
        List<WorkItem> work,
        ScoreResult?[] originals,
        ScoreResult?[] normalized
    )
    {
        if (work.Count == 0)
            return;

        int batchCount = (work.Count + _options.BatchSize - 1) / _options.BatchSize;
        Parallel.For(
            0,
            batchCount,
            new ParallelOptions { MaxDegreeOfParallelism = _options.MaxConcurrency },
            batch =>
            {
                int offset = batch * _options.BatchSize;
                int count = Math.Min(_options.BatchSize, work.Count - offset);
                float[] logits = _roberta.Infer(work, offset, count);

                for (int row = 0; row < count; row++)
                {
                    WorkItem item = work[offset + row];
                    ScoreResult result = ScoreRoberta(
                        item.Text,
                        logits.AsSpan(row * RobertaLabels.Length, RobertaLabels.Length)
                    );
                    Store(item, result, originals, normalized);
                }
            }
        );
    }

    void ScoreDistilBertBatches(
        List<WorkItem> work,
        ScoreResult?[] originals,
        ScoreResult?[] normalized
    )
    {
        if (work.Count == 0)
            return;

        const int labelCount = 2;

        int batchCount = (work.Count + _options.BatchSize - 1) / _options.BatchSize;
        Parallel.For(
            0,
            batchCount,
            new ParallelOptions { MaxDegreeOfParallelism = _options.MaxConcurrency },
            batch =>
            {
                int offset = batch * _options.BatchSize;
                int count = Math.Min(_options.BatchSize, work.Count - offset);
                float[] logits = _distilBert.Infer(work, offset, count);

                for (int row = 0; row < count; row++)
                {
                    WorkItem item = work[offset + row];
                    ScoreResult result = ScoreDistilBert(
                        item.Text,
                        logits.AsSpan(row * labelCount, labelCount)
                    );
                    Store(item, result, originals, normalized);
                }
            }
        );
    }

    static void Store(
        WorkItem item,
        ScoreResult result,
        ScoreResult?[] originals,
        ScoreResult?[] normalized
    )
    {
        if (item.IsNormalized)
            normalized[item.Index] = result;
        else
            originals[item.Index] = result;
    }

    static void Validate(ToxicityClassifierOptions options)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxLength, 2);
        ValidateThreshold(options.RobertaThreshold, nameof(options.RobertaThreshold));
        ValidateThreshold(options.InsultThreshold, nameof(options.InsultThreshold));
        ValidateThreshold(options.DistilBertThreshold, nameof(options.DistilBertThreshold));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.BatchSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxConcurrency, 1);

        foreach (
            string path in (string[])
                [
                    options.RobertaModelPath,
                    options.RobertaTokenizerPath,
                    options.DistilBertModelPath,
                    options.DistilBertTokenizerPath,
                ]
        )
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Model file not found: {path}", path);
        }
    }

    static void ValidateThreshold(double value, string name)
    {
        if (value is < 0 or > 1)
            throw new ArgumentOutOfRangeException(
                name,
                value,
                "Threshold must be between 0 and 1."
            );
    }

    internal static bool IsLatinScript(string text)
    {
        foreach (Rune rune in text.EnumerateRunes())
        {
            if (!Rune.IsLetter(rune))
                continue;

            int value = rune.Value;
            if (value is > 0x024F and (< 0x1E00 or > 0x1EFF))
                return false;
        }

        return true;
    }

    static string Normalize(string input)
    {
        string text = input.Normalize(NormalizationForm.FormKC);
        text = MixedScriptWord().Replace(text, static match => NormalizeMixedScript(match.Value));
        text = LeetspeakWord().Replace(text, static match => NormalizeLeetspeak(match.Value));
        text = EmbeddedLeetspeak().Replace(text, static match => NormalizeLeetspeak(match.Value));
        text = CanonicalizeCharacters(text);

        if (IsShouting(text))
            text = text.ToLowerInvariant();

        text = NWordEvasion().Replace(text, "nigger");
        return CollapseSpacedLetterRuns(text);
    }

    static string CanonicalizeCharacters(string text)
    {
        var output = new StringBuilder(text.Length);
        Rune previous = default;
        int repeats = 0;
        bool pendingSpace = false;

        foreach (Rune rune in text.EnumerateRunes())
        {
            UnicodeCategory category = Rune.GetUnicodeCategory(rune);
            if (
                category
                is UnicodeCategory.Control
                    or UnicodeCategory.Format
                    or UnicodeCategory.Surrogate
            )
                continue;

            if (Rune.IsWhiteSpace(rune) || IsPunctuation(category))
            {
                pendingSpace = output.Length > 0;
                previous = default;
                repeats = 0;
                continue;
            }

            if (pendingSpace)
            {
                output.Append(' ');
                pendingSpace = false;
            }

            if (Rune.IsLetter(rune) && rune == previous)
            {
                if (++repeats >= 2)
                    continue;
            }
            else
            {
                previous = rune;
                repeats = 0;
            }

            output.Append(rune);
        }

        return output.ToString();
    }

    static bool IsPunctuation(UnicodeCategory category) =>
        category
            is UnicodeCategory.ConnectorPunctuation
                or UnicodeCategory.DashPunctuation
                or UnicodeCategory.OpenPunctuation
                or UnicodeCategory.ClosePunctuation
                or UnicodeCategory.InitialQuotePunctuation
                or UnicodeCategory.FinalQuotePunctuation
                or UnicodeCategory.OtherPunctuation;

    static string NormalizeMixedScript(string word)
    {
        bool hasLatin = false;
        bool hasCyrillic = false;

        foreach (char c in word)
        {
            hasLatin |= c is >= '\u0041' and <= '\u024F';
            hasCyrillic |= c is >= '\u0400' and <= '\u052F';
        }

        if (!hasLatin || !hasCyrillic)
            return word;

        return string.Create(
            word.Length,
            word,
            static (span, source) =>
            {
                for (int i = 0; i < source.Length; i++)
                {
                    span[i] = source[i] switch
                    {
                        'а' => 'a',
                        'А' => 'A',
                        'е' => 'e',
                        'Е' => 'E',
                        'о' => 'o',
                        'О' => 'O',
                        'р' => 'p',
                        'Р' => 'P',
                        'с' => 'c',
                        'С' => 'C',
                        'х' => 'x',
                        'Х' => 'X',
                        'у' => 'y',
                        'У' => 'Y',
                        'і' => 'i',
                        'І' => 'I',
                        'ј' => 'j',
                        'Ј' => 'J',
                        'к' => 'k',
                        'К' => 'K',
                        'м' => 'm',
                        'М' => 'M',
                        'т' => 't',
                        'Т' => 'T',
                        'в' => 'b',
                        'В' => 'B',
                        'н' => 'h',
                        'Н' => 'H',
                        _ => source[i],
                    };
                }
            }
        );
    }

    static string NormalizeLeetspeak(string word) =>
        string.Create(
            word.Length,
            word,
            static (span, source) =>
            {
                for (int i = 0; i < source.Length; i++)
                {
                    span[i] = source[i] switch
                    {
                        '0' => 'o',
                        '1' => 'i',
                        '3' => 'e',
                        '4' => 'a',
                        '5' => 's',
                        '7' => 't',
                        '8' => 'b',
                        '9' => 'g',
                        '@' => 'a',
                        '$' => 's',
                        '!' => 'i',
                        '|' => 'i',
                        _ => source[i],
                    };
                }
            }
        );

    static bool IsShouting(string text)
    {
        bool hasLetter = false;

        foreach (Rune rune in text.EnumerateRunes())
        {
            if (!Rune.IsLetter(rune))
                continue;

            hasLetter = true;
            if (!Rune.IsUpper(rune))
                return false;
        }

        return hasLetter;
    }

    static bool IsSwearOnly(string text) =>
        NonLetters().Replace(Expletives().Replace(text, " "), "").Length == 0;

    static string CollapseSpacedLetterRuns(string text)
    {
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var output = new List<string>(words.Length);

        for (int i = 0; i < words.Length; )
        {
            int end = i;
            while (end < words.Length && IsRepeatedLetter(words[end]))
                end++;

            if (end - i >= 3)
            {
                output.Add(string.Concat(words[i..end].Select(static word => word[0])));
                i = end;
            }
            else
            {
                output.Add(words[i++]);
            }
        }

        return string.Join(' ', output);
    }

    static bool IsRepeatedLetter(string word)
    {
        if (word.Length == 0 || !char.IsLetter(word[0]))
            return false;

        for (int i = 1; i < word.Length; i++)
        {
            if (word[i] != word[0])
                return false;
        }

        return true;
    }

    static double Sigmoid(float value) => 1 / (1 + Math.Exp(-value));

    static double SoftmaxClassOne(ReadOnlySpan<float> logits)
    {
        if (logits.Length < 2)
            return logits.IsEmpty ? 0 : 1;

        double first = Math.Exp(logits[0] - Math.Max(logits[0], logits[1]));
        double second = Math.Exp(logits[1] - Math.Max(logits[0], logits[1]));
        return second / (first + second);
    }

    [GeneratedRegex(
        @"\b(?:fuck(?:ing|ed|s)?|shit(?:ty|s)?|damn(?:ed)?|dammit|hell|crap|wtf)\b",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex Expletives();

    [GeneratedRegex(@"[^\p{L}]+")]
    private static partial Regex NonLetters();

    [GeneratedRegex(
        @"\bn{1,3}[\W_]*[i1l!|]{1,4}[\W_]*[g69q]{1,8}[\W_]*[a4@]{1,8}\b",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex NWordEvasion();

    [GeneratedRegex(
        @"[\p{IsBasicLatin}\p{IsLatin-1Supplement}\p{IsLatinExtended-A}\p{IsLatinExtended-B}\p{IsCyrillic}]+"
    )]
    private static partial Regex MixedScriptWord();

    [GeneratedRegex(@"(?=[A-Za-z0-9@$]*[A-Za-z])[A-Za-z0-9@$]*[0-9@$][A-Za-z0-9@$]*")]
    private static partial Regex LeetspeakWord();

    [GeneratedRegex(@"(?<=[A-Za-z])[!|](?=[A-Za-z])")]
    private static partial Regex EmbeddedLeetspeak();

    public void Dispose()
    {
        if (_disposed)
            return;

        _roberta.Dispose();
        _distilBert.Dispose();
        _disposed = true;
    }

    /// <summary>Pure idle-unload gate used by model slots (unit-tested without ONNX).</summary>
    internal static bool ShouldUnloadIdle(
        DateTime lastUsedUtc,
        int inFlight,
        TimeSpan idle,
        DateTime utcNow
    )
    {
        if (idle <= TimeSpan.Zero || inFlight > 0)
            return false;
        return utcNow - lastUsedUtc >= idle;
    }

    readonly record struct WorkItem(int Index, string Text, bool IsNormalized);

    /// <summary>Lazy ONNX session: load on first Infer, unload after idle.</summary>
    sealed class LazyModelSlot : IDisposable
    {
        readonly string _name;
        readonly string _modelPath;
        readonly string _tokenizerPath;
        readonly int _padId;
        readonly int _maxLength;
        readonly int _maxConcurrency;
        readonly ILogger? _logger;
        readonly Lock _lock = new();
        ModelRuntime? _runtime;
        DateTime _lastUsedUtc = DateTime.MinValue;
        int _inFlight;
        bool _disposed;

        public LazyModelSlot(
            string name,
            string modelPath,
            string tokenizerPath,
            int padId,
            int maxLength,
            int maxConcurrency,
            ILogger? logger
        )
        {
            _name = name;
            _modelPath = modelPath;
            _tokenizerPath = tokenizerPath;
            _padId = padId;
            _maxLength = maxLength;
            _maxConcurrency = maxConcurrency;
            _logger = logger;
        }

        public bool IsLoaded
        {
            get
            {
                lock (_lock)
                    return _runtime is not null;
            }
        }

        public float[] Infer(string text)
        {
            var runtime = BeginInfer();
            try
            {
                return runtime.Infer(text);
            }
            finally
            {
                EndInfer();
            }
        }

        public float[] Infer(List<WorkItem> work, int offset, int count)
        {
            var runtime = BeginInfer();
            try
            {
                return runtime.Infer(work, offset, count);
            }
            finally
            {
                EndInfer();
            }
        }

        public void UnloadIfIdle(TimeSpan idle)
        {
            lock (_lock)
            {
                if (_runtime is null)
                    return;
                if (!ShouldUnloadIdle(_lastUsedUtc, _inFlight, idle, DateTime.UtcNow))
                    return;

                var minutes = (int)idle.TotalMinutes;
                _runtime.Dispose();
                _runtime = null;
                _logger?.LogInformation("Unloaded {Model} after {Minutes}m idle", _name, minutes);
            }
        }

        ModelRuntime BeginInfer()
        {
            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_runtime is null)
                {
                    _logger?.LogInformation("Loaded {Model} from {Path}", _name, _modelPath);
                    _runtime = new ModelRuntime(
                        _modelPath,
                        _tokenizerPath,
                        _padId,
                        _maxLength,
                        _maxConcurrency
                    );
                }

                _inFlight++;
                _lastUsedUtc = DateTime.UtcNow;
                return _runtime;
            }
        }

        void EndInfer()
        {
            lock (_lock)
            {
                _inFlight--;
                _lastUsedUtc = DateTime.UtcNow;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;
                _runtime?.Dispose();
                _runtime = null;
                _disposed = true;
            }
        }
    }

    sealed class ModelRuntime : IDisposable
    {
        readonly Lock _tokenizerLock = new();
        readonly InferenceSession _session;
        readonly Tokenizer _tokenizer;
        readonly int _padId;
        readonly int _maxLength;

        public ModelRuntime(
            string modelPath,
            string tokenizerPath,
            int padId,
            int maxLength,
            int maxConcurrency
        )
        {
            _tokenizer = new(tokenizerPath);
            try
            {
                using var sessionOptions = new SessionOptions
                {
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                    IntraOpNumThreads = Math.Max(1, Environment.ProcessorCount / maxConcurrency),
                    InterOpNumThreads = 1,
                    ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
                };
                _session = new(modelPath, sessionOptions);
            }
            catch
            {
                _tokenizer.Dispose();
                throw;
            }

            _padId = padId;
            _maxLength = maxLength;
        }

        public float[] Infer(string text)
        {
            uint[] tokens;
            lock (_tokenizerLock)
            {
                tokens = _tokenizer.Encode(text);
            }

            int length = tokens.Length;
            while (length > 2 && tokens[length - 1] == _padId)
                length--;

            var inputIds = new long[_maxLength];
            var attentionMask = new long[_maxLength];
            if (_padId != 0)
                Array.Fill(inputIds, (long)_padId);

            int count = Math.Min(length, _maxLength);

            for (int i = 0; i < count; i++)
            {
                inputIds[i] = tokens[i];
                attentionMask[i] = 1;
            }

            if (length > _maxLength)
                inputIds[^1] = tokens[length - 1];

            int[] shape = [1, _maxLength];
            NamedOnnxValue[] inputs =
            [
                NamedOnnxValue.CreateFromTensor(
                    "input_ids",
                    new DenseTensor<long>(inputIds, shape)
                ),
                NamedOnnxValue.CreateFromTensor(
                    "attention_mask",
                    new DenseTensor<long>(attentionMask, shape)
                ),
            ];

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(
                inputs
            );
            return [.. results[0].AsEnumerable<float>()];
        }

        public float[] Infer(List<WorkItem> work, int offset, int count)
        {
            int tensorLength = count * _maxLength;
            var inputIds = new long[tensorLength];
            var attentionMask = new long[tensorLength];

            if (_padId != 0)
                Array.Fill(inputIds, (long)_padId);

            for (int row = 0; row < count; row++)
            {
                uint[] tokens;
                lock (_tokenizerLock)
                {
                    tokens = _tokenizer.Encode(work[offset + row].Text);
                }

                int length = tokens.Length;
                while (length > 2 && tokens[length - 1] == _padId)
                    length--;

                int tokenCount = Math.Min(length, _maxLength);
                int rowStart = row * _maxLength;

                for (int token = 0; token < tokenCount; token++)
                {
                    inputIds[rowStart + token] = tokens[token];
                    attentionMask[rowStart + token] = 1;
                }

                if (length > _maxLength)
                    inputIds[rowStart + _maxLength - 1] = tokens[length - 1];
            }

            int[] shape = [count, _maxLength];
            NamedOnnxValue[] inputs =
            [
                NamedOnnxValue.CreateFromTensor(
                    "input_ids",
                    new DenseTensor<long>(inputIds, shape)
                ),
                NamedOnnxValue.CreateFromTensor(
                    "attention_mask",
                    new DenseTensor<long>(attentionMask, shape)
                ),
            ];

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(
                inputs
            );
            return [.. results[0].AsEnumerable<float>()];
        }

        public void Dispose()
        {
            _session.Dispose();
            _tokenizer.Dispose();
        }
    }
}
