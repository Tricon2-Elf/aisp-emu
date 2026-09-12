using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game;

public enum WordFilterLevel
{
    Allowed,
    NoSlurs,
    Complete,
}

public interface IWordFilter
{
    bool ContainsBlockedWord(WordFilterLevel level, params string[] texts);
}

/// <summary>
/// File-backed banned-word list for player-authored text.
/// <see cref="WordFilterLevel.Complete"/> uses swears and slurs (names, rooms, profiles, mail).
/// <see cref="WordFilterLevel.NoSlurs"/> uses slurs only so chat can allow swearing.
/// <see cref="WordFilterLevel.Allowed"/> never blocks.
/// Matching is case-insensitive substring search after leetspeak-aware normalization
/// (strip separators, map 4→a, 0→o, etc.).
/// </summary>
public sealed class WordFilter : IWordFilter
{
    public static string DefaultListPath =>
        Path.Combine(AppContext.BaseDirectory, "seedData", "blockedWords.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly IReadOnlyList<string> _blockedTerms;
    private readonly IReadOnlyList<string> _slurTerms;

    [ActivatorUtilitiesConstructor]
    public WordFilter(ILogger<WordFilter> logger)
        : this(DefaultListPath, logger) { }

    public WordFilter(string filePath, ILogger<WordFilter>? logger = null)
    {
        var loaded = LoadTerms(filePath, logger);
        _blockedTerms = loaded.Blocked;
        _slurTerms = loaded.Slurs;
    }

    /// <summary>In-memory list for tests. Not used by DI (avoids IEnumerable&lt;string&gt; ambiguity).</summary>
    public static WordFilter FromTerms(IEnumerable<string> blockedTerms) =>
        FromTerms(blockedTerms, blockedTerms);

    public static WordFilter FromTerms(
        IEnumerable<string> blockedTerms,
        IEnumerable<string> slurTerms
    ) => new(blockedTerms.ToArray(), slurTerms.ToArray());

    private WordFilter(string[] blockedTerms, string[] slurTerms)
    {
        _blockedTerms = DistinctNormalized(blockedTerms);
        _slurTerms = DistinctNormalized(slurTerms);
    }

    public bool ContainsBlockedWord(WordFilterLevel level, params string[] texts)
    {
        if (level == WordFilterLevel.Allowed || texts.Length == 0)
            return false;

        var terms = level == WordFilterLevel.NoSlurs ? _slurTerms : _blockedTerms;
        foreach (var text in texts)
        {
            if (ContainsAnyTerm(text, terms))
                return true;
        }

        return false;
    }

    private static bool ContainsAnyTerm(string text, IReadOnlyList<string> terms)
    {
        if (terms.Count == 0 || string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = Normalize(text);
        if (normalized.Length == 0)
            return false;

        foreach (var term in terms)
        {
            if (normalized.Contains(term, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    internal static string Normalize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        Span<char> buffer =
            text.Length <= 256 ? stackalloc char[text.Length] : new char[text.Length];
        var length = 0;
        foreach (var ch in text)
        {
            if (!TryMapNormalizedChar(ch, out var mapped))
                continue;

            buffer[length++] = mapped;
        }

        return length == 0 ? string.Empty : new string(buffer[..length]);
    }

    /// <summary>
    /// Maps letters and common leetspeak substitutes (4→a, 0→o, @→a, …) to lowercase letters.
    /// Other characters are ignored.
    /// </summary>
    private static bool TryMapNormalizedChar(char ch, out char mapped)
    {
        mapped = char.ToLowerInvariant(ch);
        if (mapped is >= 'a' and <= 'z')
            return true;

        mapped = ch switch
        {
            '0' or '⁰' => 'o',
            '1' or '!' or '|' => 'i',
            '2' => 'z',
            '3' => 'e',
            '4' or '@' or 'ª' => 'a',
            '5' or '$' => 's',
            '6' => 'g',
            '7' or '+' => 't',
            '8' => 'b',
            '9' => 'g',
            _ => '\0',
        };
        return mapped != '\0';
    }

    internal static (IReadOnlyList<string> Blocked, IReadOnlyList<string> Slurs) ParseTermLists(
        string content
    )
    {
        var file = JsonSerializer.Deserialize<BlockedWordsFile>(content, JsonOptions);
        var slurs = DistinctNormalized(file?.Slurs ?? []);
        var swears = DistinctNormalized(file?.Swears ?? []);
        return (DistinctNormalized(swears.Concat(slurs)), slurs);
    }

    private static IReadOnlyList<string> DistinctNormalized(IEnumerable<string> terms) =>
        terms
            .Select(Normalize)
            .Where(term => term.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static (IReadOnlyList<string> Blocked, IReadOnlyList<string> Slurs) LoadTerms(
        string filePath,
        ILogger<WordFilter>? logger
    )
    {
        try
        {
            if (!File.Exists(filePath))
            {
                logger?.LogWarning(
                    "Banned-words list not found at {Path}; word filter is inactive",
                    filePath
                );
                return ([], []);
            }

            return FinalizeTerms(ParseTermLists(File.ReadAllText(filePath)), filePath, logger);
        }
        catch (Exception ex)
        {
            logger?.LogError(
                ex,
                "Failed to load banned-words list {Path}; word filter is inactive",
                filePath
            );
            return ([], []);
        }
    }

    private static (IReadOnlyList<string> Blocked, IReadOnlyList<string> Slurs) FinalizeTerms(
        (IReadOnlyList<string> Blocked, IReadOnlyList<string> Slurs) terms,
        string filePath,
        ILogger<WordFilter>? logger
    )
    {
        if (terms.Blocked.Count == 0)
        {
            logger?.LogWarning(
                "Banned-words list {Path} contained no usable terms; word filter is inactive",
                filePath
            );
        }
        else
        {
            logger?.LogInformation(
                "Loaded {Count} banned-word terms ({SlurCount} chat slurs) from {Path}",
                terms.Blocked.Count,
                terms.Slurs.Count,
                filePath
            );
        }

        return terms;
    }

    private sealed class BlockedWordsFile
    {
        [JsonPropertyName("swears")]
        public string[]? Swears { get; set; }

        [JsonPropertyName("slurs")]
        public string[]? Slurs { get; set; }
    }
}
