using System.Globalization;
using System.Text;

namespace aisp.Common.Services.Toxicity;

public sealed record LabelScore(string Name, double Score, bool CountsForVerdict);

public sealed record ScoreResult(string Text, bool IsToxic, IReadOnlyList<LabelScore> Scores);

public sealed record ToxicityResult(string Model, ScoreResult Original, ScoreResult? Normalized)
{
    // Normalization intentionally supersedes the raw score. This prevents all-caps
    // false positives while still exposing Original for diagnostics.
    public bool IsToxic => Normalized?.IsToxic ?? Original.IsToxic;

    public ScoreResult Verdict => Normalized ?? Original;
}

public sealed record ToxicityClassifierOptions(
    string RobertaModelPath,
    string RobertaTokenizerPath,
    string DistilBertModelPath,
    string DistilBertTokenizerPath
)
{
    public int MaxLength { get; init; } = 128;
    public double RobertaThreshold { get; init; } = 0.50;
    public double InsultThreshold { get; init; } = 0.40;
    public double DistilBertThreshold { get; init; } = 0.85;
    public int BatchSize { get; init; } = 1;
    public int MaxConcurrency { get; init; } = Math.Clamp(Environment.ProcessorCount / 2, 1, 8);

    public string LidModelPath { get; init; } = "";
    public string LidVocabPath { get; init; } = "";
    public string LidConfigPath { get; init; } = "";
    public string LidLabelsPath { get; init; } = "";
    public string LidHsTreePath { get; init; } = "";

    public static ToxicityClassifierOptions FromModelRoot(string modelRoot = "models") =>
        new(
            Path.Combine(modelRoot, "unbiased-roberta", "onnx", "model_quantized.onnx"),
            Path.Combine(modelRoot, "unbiased-roberta", "tokenizer.json"),
            Path.Combine(modelRoot, "distilbert", "onnx", "model_quantized.onnx"),
            Path.Combine(modelRoot, "distilbert", "tokenizer.json")
        )
        {
            LidModelPath = Path.Combine(modelRoot, "lid176", "onnx", "lid176.int8.onnx"),
            LidVocabPath = Path.Combine(modelRoot, "lid176", "vocab.txt"),
            LidConfigPath = Path.Combine(modelRoot, "lid176", "config.json"),
            LidLabelsPath = Path.Combine(modelRoot, "lid176", "labels.json"),
            LidHsTreePath = Path.Combine(modelRoot, "lid176", "hs_tree.json"),
        };
}

/// <summary>
/// Formats ToxicityReason from label scores only (no model name / verdict words).
/// Empty string means never classified.
/// </summary>
public static class ChatToxicityReason
{
    public const int MaxLength = 1024;

    public static string Format(ToxicityResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var scores = result.Verdict.Scores;
        if (scores.Count == 0)
            return string.Empty;

        var sb = new StringBuilder(scores.Count * 24);
        for (var i = 0; i < scores.Count; i++)
        {
            if (i > 0)
                sb.Append("; ");
            sb.Append(scores[i].Name);
            sb.Append(' ');
            sb.Append(
                ((int)Math.Round(scores[i].Score * 100)).ToString(CultureInfo.InvariantCulture)
            );
            sb.Append('%');
        }

        if (sb.Length <= MaxLength)
            return sb.ToString();
        return sb.ToString(0, MaxLength);
    }
}
