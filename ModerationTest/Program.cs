using System.Runtime.InteropServices;
using System.Text;
using aisp.Common.Services.Toxicity;

UseUtf8Console();

Console.WriteLine("Loading models...");
using var classifier = ToxicityClassifier.Load();

switch (args)
{
    case ["--chatlog"]:
        RunChatlog(classifier, "chatlog.txt");
        break;
    case ["--chatlog", string path]:
        RunChatlog(classifier, path);
        break;
    case [string path] when File.Exists(path):
        RunChatlog(classifier, path);
        break;
    case [_, ..]:
        foreach (string sample in args)
            Print(classifier.Classify(sample));
        break;
    default:
        RunInteractive(classifier);
        break;
}

static void RunInteractive(ToxicityClassifier classifier)
{
    Console.WriteLine("Ready. English -> RoBERTa, other scripts -> DistilBERT. 'exit' to quit.");

    while (true)
    {
        Console.Write("> ");
        string? text = Console.ReadLine();

        if (text is null || text.Equals("exit", StringComparison.OrdinalIgnoreCase))
            return;
        if (!string.IsNullOrWhiteSpace(text))
            Print(classifier.Classify(text));
    }
}

static void RunChatlog(ToxicityClassifier classifier, string path)
{
    if (!File.Exists(path))
    {
        Console.WriteLine($"Chat log not found: {path}");
        return;
    }

    (string User, string Message)[] entries = [.. ReadChatlog(path)];
    string[] messages = [.. entries.Select(static entry => entry.Message)];
    ToxicityResult[] results = classifier.ClassifyMany(messages);
    var flagged = new List<ToxicityResult>();

    Console.WriteLine($"Testing {path}");
    Console.WriteLine();

    for (int i = 0; i < entries.Length; i++)
    {
        var (user, message) = entries[i];
        ToxicityResult result = results[i];
        string who = string.IsNullOrEmpty(user) ? message : $"{user}: {message}";

        Console.Write($"{(result.IsToxic ? "TOXIC    " : "         ")}{who}");
        if (result.IsToxic)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("  -> TOXIC");
            Console.ResetColor();
            if (result.Normalized is { } normalized)
                Console.Write($"  (as \"{normalized.Text}\")");
            flagged.Add(result);
        }

        Console.WriteLine();
    }

    Console.WriteLine();
    Console.WriteLine($"Flagged {flagged.Count} / {entries.Length}");
    Console.WriteLine();
    foreach (var result in flagged)
        Print(result);
}

static IEnumerable<(string User, string Message)> ReadChatlog(string path)
{
    foreach (string line in File.ReadLines(path, Encoding.UTF8))
    {
        if (string.IsNullOrWhiteSpace(line))
            continue;

        string[] parts = line.Split('\t', StringSplitOptions.TrimEntries);
        int start = 0;
        if (parts.Length > 0 && LooksLikeTimestamp(parts[0]))
            start = 1;

        int end = parts.Length;
        if (end > start && parts[end - 1] is "Sent" or "Received" or "Rejected")
            end--;

        if (end - start >= 2)
            yield return (parts[start], string.Join('\t', parts[(start + 1)..end]));
        else if (end - start == 1)
            yield return ("", parts[start]);
    }
}

static bool LooksLikeTimestamp(string value) =>
    value.Length >= 19 && char.IsDigit(value[0]) && value[4] == '-';

static void Print(ToxicityResult result)
{
    ScoreResult scored = result.Verdict;
    string top = string.Join(
        ", ",
        scored
            .Scores.OrderByDescending(static score => score.Score)
            .Take(3)
            .Select(s => $"{s.Name} {s.Score:P0}")
    );

    Console.Write($"({result.Model}) \"{result.Original.Text}\" -> ");
    Console.ForegroundColor = result.IsToxic ? ConsoleColor.Red : ConsoleColor.Green;
    Console.Write(result.IsToxic ? "TOXIC" : "NOT TOXIC");
    Console.ResetColor();
    Console.WriteLine($" ({top})");
}

static void UseUtf8Console()
{
    Console.InputEncoding = Encoding.UTF8;
    Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    if (OperatingSystem.IsWindows())
    {
        SetConsoleCP(65001);
        SetConsoleOutputCP(65001);
    }
}

[DllImport("kernel32.dll")]
static extern bool SetConsoleCP(uint codePage);

[DllImport("kernel32.dll")]
static extern bool SetConsoleOutputCP(uint codePage);
