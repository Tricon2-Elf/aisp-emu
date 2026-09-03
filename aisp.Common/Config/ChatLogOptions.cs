namespace aisp.Common.Config;

public class ChatLogOptions
{
    public const string SectionName = "ChatLog";

    /// <summary>Chat rows older than this many days are deleted. Set to 0 to disable pruning.</summary>
    public int RetentionDays { get; set; } = 60;
}
