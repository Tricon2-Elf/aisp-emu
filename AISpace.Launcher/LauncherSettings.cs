using System.Text.Json;
using System.Text.Json.Serialization;

namespace AISpace.Launcher;

public sealed class LauncherSettings
{
    public const string DefaultWebsiteUrl = "https://aisp.moe";
    public const string DefaultGameExecutable = "ai sp@ce.exe";

    public string WebsiteUrl { get; set; } = DefaultWebsiteUrl;

    public string GameExecutable { get; set; } = DefaultGameExecutable;

    /// <summary>
    /// When true on Windows, launcher injects aisp.localehook.dll into the game process
    /// to emulate Japanese ACP/locale without external locale emulator tools.
    /// </summary>
    public bool UseLocaleReplacer { get; set; } = true;

    public GameEnvironment SelectedEnvironment { get; set; } = GameEnvironment.Stable;

    public Dictionary<string, EnvironmentSettings> Environments { get; set; } =
        new()
        {
            [nameof(GameEnvironment.Stable)] = new()
            {
                AuthHost = "aisp.moe",
                DownloadHost = "aisp.moe",
                DownloadPath = "ai-sp/download.php",
                UploadHost = "aisp.moe",
                UploadPath = "ai-sp/upload.php",
            },
            [nameof(GameEnvironment.Dev)] = new()
            {
                AuthHost = "aisp.moe",
                DownloadHost = "aisp.moe",
                DownloadPath = "ai-sp/dev/download.php",
                UploadHost = "aisp.moe",
                UploadPath = "ai-sp/dev/upload.php",
            },
            [nameof(GameEnvironment.Local)] = new()
            {
                AuthHost = "127.0.0.1",
                DownloadHost = "127.0.0.1",
                DownloadPath = "ai-sp/dev/download.php",
                UploadHost = "127.0.0.1",
                UploadPath = "ai-sp/dev/upload.php",
            },
        };

    public static string GetPath() => Path.Combine(AppContext.BaseDirectory, "launcher.settings.json");

    public static LauncherSettings LoadOrCreate()
    {
        var path = GetPath();
        if (!File.Exists(path))
        {
            var defaults = new LauncherSettings();
            defaults.Save(path);
            return defaults;
        }

        return Load(path);
    }

    public static LauncherSettings Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<LauncherSettings>(json, JsonOptions) ?? new LauncherSettings();
    }

    public void Save() => Save(GetPath());

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }

    public EnvironmentSettings GetEnvironment(GameEnvironment environment) => Environments.TryGetValue(environment.ToString(), out var settings) ? settings : new EnvironmentSettings();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
}

public sealed class EnvironmentSettings
{
    public string AuthHost { get; set; } = "aisp.moe";

    public ushort AuthPort { get; set; } = 50050;

    public int BypassNicoLogin { get; set; } = 1;

    public string DownloadHost { get; set; } = "aisp.moe";

    public string DownloadPath { get; set; } = "ai-sp/dev/download.php";

    public string UploadHost { get; set; } = "aisp.moe";

    public string UploadPath { get; set; } = "ai-sp/dev/upload.php";
}
