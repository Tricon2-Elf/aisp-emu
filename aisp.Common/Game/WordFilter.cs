using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game;

public interface IWordFilter
{
    bool ContainsBlockedWord(string text);
}

/// <summary>
/// File-backed blocked-word list for names (and later chat). Matching is case-insensitive
/// substring search after leetspeak-aware normalization (strip separators, map 4→a, 0→o, etc.).
/// When the local cache is missing, seeds from the public profanity list and writes it locally.
/// </summary>
public sealed class WordFilter : IWordFilter
{
    public const string DefaultSourceUrl =
        "https://raw.githubusercontent.com/dsojevic/profanity-list/main/en.txt";

    public static string DefaultCachePath =>
        Path.Combine(AppContext.BaseDirectory, "seedData", "blockedWords.txt");

    private readonly IReadOnlyList<string> _blockedTerms;

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
        _blockedTerms = LoadTerms(filePath, logger, fetchRemote);
    }

    /// <summary>In-memory list for tests. Not used by DI (avoids IEnumerable&lt;string&gt; ambiguity).</summary>
    public static WordFilter FromTerms(IEnumerable<string> blockedTerms) =>
        new(blockedTerms.ToArray());

    private WordFilter(string[] blockedTerms)
    {
        _blockedTerms = blockedTerms
            .Select(Normalize)
            .Where(term => term.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public bool ContainsBlockedWord(string text)
    {
        if (_blockedTerms.Count == 0 || string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = Normalize(text);
        if (normalized.Length == 0)
            return false;

        foreach (var term in _blockedTerms)
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

        return terms.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<string> LoadTerms(
        string filePath,
        ILogger<WordFilter>? logger,
        Func<string, ILogger<WordFilter>?, string?>? fetchRemote
    )
    {
        try
        {
            if (!File.Exists(filePath))
            {
                if (fetchRemote is null)
                {
                    logger?.LogWarning(
                        "Blocked-words cache not found at {Path} and remote seeding is disabled; name filter is inactive",
                        filePath
                    );
                    return [];
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
                        "Remote blocked-words download returned no content; name filter is inactive"
                    );
                    return [];
                }

                TryWriteCache(filePath, remote, logger);
                return FinalizeTerms(ParseTerms(remote), filePath, logger);
            }

            var local = File.ReadAllText(filePath);
            return FinalizeTerms(ParseTerms(local), filePath, logger);
        }
        catch (Exception ex)
        {
            logger?.LogError(
                ex,
                "Failed to load blocked-words cache {Path}; name filter is inactive",
                filePath
            );
            return [];
        }
    }

    private static IReadOnlyList<string> FinalizeTerms(
        IReadOnlyList<string> terms,
        string filePath,
        ILogger<WordFilter>? logger
    )
    {
        if (terms.Count == 0)
        {
            logger?.LogWarning(
                "Blocked-words cache {Path} contained no usable terms; name filter is inactive",
                filePath
            );
        }
        else
        {
            logger?.LogInformation(
                "Loaded {Count} blocked-word terms from {Path}",
                terms.Count,
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
}
