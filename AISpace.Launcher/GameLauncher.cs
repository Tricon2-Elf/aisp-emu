using System.Diagnostics;

namespace AISpace.Launcher;

public sealed class GameLauncher(LauncherSettings settings)
{
    public string SettingsPath { get; } = LauncherSettings.GetPath();

    public LauncherSettings Settings { get; } = settings;

    public GameLaunchResult TryLaunch(GameEnvironment environment)
    {
        var envSettings = Settings.GetEnvironment(environment);
        var executable = ResolveExecutablePath(Settings.GameExecutable);
        if (executable is null)
        {
            return GameLaunchResult.Failure("Game client not found.", $"Place Launcher in the same directory as the game client.");
        }

        try
        {
            var gameDirectory = Path.GetDirectoryName(executable) ?? AppContext.BaseDirectory;
            ConnectionFile.Write(gameDirectory, envSettings);

            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = "./data",
                UseShellExecute = false,
                WorkingDirectory = gameDirectory,
            };

            Process.Start(startInfo);
            return GameLaunchResult.Success(executable);
        }
        catch (Exception ex)
        {
            return GameLaunchResult.Failure("Failed to start the game client.", ex.Message);
        }
    }

    private static string? ResolveExecutablePath(string fileName)
    {
        var resolved = Path.IsPathRooted(fileName) ? fileName : Path.Combine(AppContext.BaseDirectory, fileName);
        return File.Exists(resolved) ? Path.GetFullPath(resolved) : null;
    }
}

public readonly record struct GameLaunchResult(bool Succeeded, string Message, string? Details = null)
{
    public static GameLaunchResult Success(string executable) => new(true, $"Started {Path.GetFileName(executable)}");

    public static GameLaunchResult Failure(string message, string? details = null) => new(false, message, details);
}
