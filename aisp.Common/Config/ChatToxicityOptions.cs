namespace aisp.Common.Config;

public class ChatToxicityOptions
{
    public const string SectionName = "ChatToxicity";

    /// <summary>When false, models are not downloaded and chat is not classified.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Directory that holds unbiased-roberta/ and distilbert/. Empty resolves to
    /// {SQLite DataSource directory}/models.
    /// </summary>
    public string ModelRoot { get; set; } = "";

    /// <summary>
    /// Minutes of idle before a loaded ONNX session is disposed. 0 keeps sessions
    /// resident until process stop.
    /// </summary>
    public int IdleUnloadMinutes { get; set; } = 15;

    /// <summary>Unclassified historical rows per ClassifyMany pass (newest first).</summary>
    public int BackfillBatchSize { get; set; } = 16;

    /// <summary>
    /// Pause after each backfill batch. 0 disables historical backfill. Live chat
    /// is never delayed by this value.
    /// </summary>
    public int BackfillDelayMs { get; set; } = 50;

    public double RobertaThreshold { get; set; } = 0.50;
    public double InsultThreshold { get; set; } = 0.40;
    public double DistilBertThreshold { get; set; } = 0.85;
}
