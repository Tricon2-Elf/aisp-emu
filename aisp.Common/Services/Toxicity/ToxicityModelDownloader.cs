using Microsoft.Extensions.Logging;

namespace aisp.Common.Services.Toxicity;

/// <summary>
/// Downloads pinned ONNX model files into <see cref="ToxicityClassifierOptions.FromModelRoot"/> layout.
/// </summary>
public sealed class ToxicityModelDownloader(
    HttpClient http,
    ILogger<ToxicityModelDownloader>? logger = null
)
{
    // protectai/unbiased-toxic-roberta-onnx — files at repo root, mapped into unbiased-roberta/onnx/
    const string RobertaRepo = "protectai/unbiased-toxic-roberta-onnx";
    const string RobertaSha = "16fd63c0e51000f407fb7d28ce655e41495ebc8b";

    // onnx-community/distilbert-multilingual-toxicity-classifier-ONNX
    const string DistilBertRepo = "onnx-community/distilbert-multilingual-toxicity-classifier-ONNX";
    const string DistilBertSha = "4fbaccee8caaba02641b1757f7ef697e3fbffdb8";

    static readonly (string Url, string RelativePath)[] Files =
    [
        (
            HfUrl(RobertaRepo, RobertaSha, "model_quantized.onnx"),
            Path.Combine("unbiased-roberta", "onnx", "model_quantized.onnx")
        ),
        (
            HfUrl(RobertaRepo, RobertaSha, "tokenizer.json"),
            Path.Combine("unbiased-roberta", "tokenizer.json")
        ),
        (
            HfUrl(DistilBertRepo, DistilBertSha, "onnx/model_quantized.onnx"),
            Path.Combine("distilbert", "onnx", "model_quantized.onnx")
        ),
        (
            HfUrl(DistilBertRepo, DistilBertSha, "tokenizer.json"),
            Path.Combine("distilbert", "tokenizer.json")
        ),
    ];

    public static IReadOnlyList<(string Url, string RelativePath)> RequiredFiles => Files;

    public async Task EnsureModelsAsync(string modelRoot, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelRoot);
        Directory.CreateDirectory(modelRoot);

        foreach (var (url, relative) in Files)
        {
            var dest = Path.Combine(modelRoot, relative);
            if (File.Exists(dest) && new FileInfo(dest).Length > 0)
            {
                logger?.LogDebug("Toxicity model already present: {Path}", dest);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            var tmp = dest + ".tmp";
            try
            {
                logger?.LogInformation("Downloading toxicity model {File}...", relative);
                await using (var response = await http.GetStreamAsync(url, ct))
                await using (
                    var file = new FileStream(
                        tmp,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        81920,
                        useAsync: true
                    )
                )
                {
                    await response.CopyToAsync(file, ct);
                    await file.FlushAsync(ct);
                }

                if (File.Exists(dest))
                    File.Delete(dest);
                File.Move(tmp, dest);
                logger?.LogInformation("Downloaded toxicity model {File}", relative);
            }
            catch
            {
                try
                {
                    if (File.Exists(tmp))
                        File.Delete(tmp);
                }
                catch
                {
                    // best-effort cleanup
                }

                throw;
            }
        }
    }

    static string HfUrl(string repo, string sha, string path) =>
        $"https://huggingface.co/{repo}/resolve/{sha}/{path}";
}
