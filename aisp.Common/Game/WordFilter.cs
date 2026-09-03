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
/// File-backed blocked-word list for player-authored text.
/// <see cref="WordFilterLevel.Complete"/> uses the full list (names, rooms, profiles, mail).
/// <see cref="WordFilterLevel.NoSlurs"/> uses a slur-only subset so chat can allow swearing.
/// <see cref="WordFilterLevel.Allowed"/> never blocks.
/// Matching is case-insensitive substring search after leetspeak-aware normalization
/// (strip separators, map 4→a, 0→o, etc.).
/// When the local cache is missing, seeds from the public profanity JSON and writes it locally.
/// </summary>
public sealed class WordFilter : IWordFilter
{
    public const string DefaultSourceUrl =
        "https://raw.githubusercontent.com/dsojevic/profanity-list/main/en.json";

    public static string DefaultCachePath =>
        Path.Combine(AppContext.BaseDirectory, "seedData", "blockedWords.json");

    public static string DefaultPolicyPath =>
        Path.Combine(AppContext.BaseDirectory, "seedData", "wordFilter.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IReadOnlyList<string> _blockedTerms;
    private readonly IReadOnlyList<string> _slurTerms;

    [ActivatorUtilitiesConstructor]
    public WordFilter(ILogger<WordFilter> logger)
        : this(DefaultCachePath, logger, DefaultFetchRemote) { }

    public WordFilter(string filePath, ILogger<WordFilter>? logger = null)
        : this(filePath, logger, DefaultFetchRemote) { }

    /// <summary>
    /// Test/DI helper. Pass <paramref name="fetchRemote"/> as null to skip network seeding.
    /// </summary>
    public WordFilter(
        string filePath,
        ILogger<WordFilter>? logger,
        Func<string, ILogger<WordFilter>?, string?>? fetchRemote
    )
    {
        var policy = LoadPolicy(DefaultPolicyPath, logger);
        var loaded = LoadTerms(filePath, logger, fetchRemote, policy);
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

    internal static IReadOnlyList<string> ParseTerms(string content)
    {
        var lists = ParseTermLists(content);
        return lists.Blocked;
    }

    internal static (IReadOnlyList<string> Blocked, IReadOnlyList<string> Slurs) ParseTermLists(
        string content,
        WordFilterPolicy? policy = null
    )
    {
        policy ??= LoadPolicy(DefaultPolicyPath, logger: null);
        if (LooksLikeJsonArray(content))
            return ParseJsonTermLists(content, policy);

        var terms = DistinctNormalized(ParsePlainTextTerms(content));
        return (terms, terms);
    }

    internal static bool IsChatSlurEntry(
        string? id,
        IEnumerable<string>? tags,
        WordFilterPolicy policy
    )
    {
        if (!string.IsNullOrEmpty(id) && policy.ChatAllowedIds.Contains(id))
            return false;

        if (!string.IsNullOrEmpty(id) && policy.ChatExtraSlurIds.Contains(id))
            return true;

        if (tags is null)
            return false;

        foreach (var tag in tags)
        {
            if (policy.ChatSlurTags.Contains(tag))
                return true;
        }

        return false;
    }

    internal static WordFilterPolicy LoadPolicy(string filePath, ILogger<WordFilter>? logger)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                logger?.LogWarning(
                    "Word-filter policy not found at {Path}; chat slur tags will not be applied",
                    filePath
                );
                return WordFilterPolicy.Empty;
            }

            return ParsePolicy(File.ReadAllText(filePath));
        }
        catch (Exception ex)
        {
            logger?.LogError(
                ex,
                "Failed to load word-filter policy {Path}; chat slur tags will not be applied",
                filePath
            );
            return WordFilterPolicy.Empty;
        }
    }

    internal static WordFilterPolicy ParsePolicy(string content)
    {
        var file = JsonSerializer.Deserialize<WordFilterPolicyFile>(content, JsonOptions);
        var chat = file?.Chat;
        return new WordFilterPolicy(
            ToIdSet(chat?.SlurTags),
            ToIdSet(chat?.ExtraSlurIds),
            ToIdSet(chat?.AllowedIds)
        );
    }

    private static IReadOnlyList<string> ParsePlainTextTerms(string content)
    {
        var terms = new List<string>();
        using var reader = new StringReader(content);
        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var normalized = Normalize(trimmed);
            if (normalized.Length > 0)
                terms.Add(normalized);
        }

        return terms;
    }

    private static (IReadOnlyList<string> Blocked, IReadOnlyList<string> Slurs) ParseJsonTermLists(
        string content,
        WordFilterPolicy policy
    )
    {
        var entries = JsonSerializer.Deserialize<ProfanityJsonEntry[]>(content, JsonOptions);
        if (entries is null || entries.Length == 0)
            return ([], []);

        var blocked = new List<string>();
        var slurs = new List<string>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Match))
                continue;

            var isSlur = IsChatSlurEntry(entry.Id, entry.Tags, policy);
            foreach (var variant in entry.Match.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                var normalized = Normalize(variant);
                if (normalized.Length == 0)
                    continue;

                blocked.Add(normalized);
                if (isSlur)
                    slurs.Add(normalized);
            }
        }

        return (DistinctNormalized(blocked), DistinctNormalized(slurs));
    }

    private static bool LooksLikeJsonArray(string content)
    {
        foreach (var ch in content)
        {
            if (char.IsWhiteSpace(ch))
                continue;
            return ch == '[';
        }

        return false;
    }

    private static IReadOnlyList<string> DistinctNormalized(IEnumerable<string> terms) =>
        terms
            .Select(Normalize)
            .Where(term => term.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static HashSet<string> ToIdSet(IEnumerable<string>? values) =>
        new(
            (values ?? []).Where(value => !string.IsNullOrWhiteSpace(value)),
            StringComparer.OrdinalIgnoreCase
        );

    private static (IReadOnlyList<string> Blocked, IReadOnlyList<string> Slurs) LoadTerms(
        string filePath,
        ILogger<WordFilter>? logger,
        Func<string, ILogger<WordFilter>?, string?>? fetchRemote,
        WordFilterPolicy policy
    )
    {
        try
        {
            if (!File.Exists(filePath))
            {
                if (fetchRemote is null)
                {
                    logger?.LogWarning(
                        "Blocked-words cache not found at {Path} and remote seeding is disabled; word filter is inactive",
                        filePath
                    );
                    return ([], []);
                }

                logger?.LogInformation(
                    "Blocked-words cache missing at {Path}; downloading from {Url}",
                    filePath,
                    DefaultSourceUrl
                );

                var remote = fetchRemote(DefaultSourceUrl, logger);
                if (string.IsNullOrWhiteSpace(remote))
                {
                    logger?.LogWarning(
                        "Remote blocked-words download returned no content; word filter is inactive"
                    );
                    return ([], []);
                }

                TryWriteCache(filePath, remote, logger);
                return FinalizeTerms(ParseTermLists(remote, policy), filePath, logger);
            }

            var local = File.ReadAllText(filePath);
            return FinalizeTerms(ParseTermLists(local, policy), filePath, logger);
        }
        catch (Exception ex)
        {
            logger?.LogError(
                ex,
                "Failed to load blocked-words cache {Path}; word filter is inactive",
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
                "Blocked-words cache {Path} contained no usable terms; word filter is inactive",
                filePath
            );
        }
        else
        {
            logger?.LogInformation(
                "Loaded {Count} blocked-word terms ({SlurCount} chat slurs) from {Path}",
                terms.Blocked.Count,
                terms.Slurs.Count,
                filePath
            );
        }

        return terms;
    }

    private static void TryWriteCache(string filePath, string content, ILogger<WordFilter>? logger)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(filePath, content);
            logger?.LogInformation("Cached blocked-words list to {Path}", filePath);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(
                ex,
                "Could not write blocked-words cache to {Path}; using in-memory list for this run",
                filePath
            );
        }
    }

    private static string? DefaultFetchRemote(string url, ILogger<WordFilter>? logger)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("aisp-emu-wordfilter/1.0");
            using var response = client.Send(request);
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to download blocked-words list from {Url}", url);
            return null;
        }
    }

    private sealed class ProfanityJsonEntry
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("match")]
        public string? Match { get; set; }

        [JsonPropertyName("tags")]
        public string[]? Tags { get; set; }
    }

    private sealed class WordFilterPolicyFile
    {
        [JsonPropertyName("chat")]
        public ChatPolicySection? Chat { get; set; }
    }

    private sealed class ChatPolicySection
    {
        [JsonPropertyName("slurTags")]
        public string[]? SlurTags { get; set; }

        [JsonPropertyName("extraSlurIds")]
        public string[]? ExtraSlurIds { get; set; }

        [JsonPropertyName("allowedIds")]
        public string[]? AllowedIds { get; set; }
    }
}

internal sealed class WordFilterPolicy(
    IReadOnlySet<string> chatSlurTags,
    IReadOnlySet<string> chatExtraSlurIds,
    IReadOnlySet<string> chatAllowedIds
)
{
    public static WordFilterPolicy Empty { get; } =
        new(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        );

    public IReadOnlySet<string> ChatSlurTags { get; } = chatSlurTags;
    public IReadOnlySet<string> ChatExtraSlurIds { get; } = chatExtraSlurIds;
    public IReadOnlySet<string> ChatAllowedIds { get; } = chatAllowedIds;
}
