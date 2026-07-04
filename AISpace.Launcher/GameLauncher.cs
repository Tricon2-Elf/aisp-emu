using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AISpace.Launcher;

public sealed class GameLauncher(LauncherSettings settings)
{
    private const string LocaleHookLibrary = "aisp.localehook.dll";

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

            var gameArgs = "./data";
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = gameArgs,
                UseShellExecute = false,
                WorkingDirectory = gameDirectory,
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Settings.UseLocaleReplacer)
            {
                var hookDll = ResolveExecutablePath(LocaleHookLibrary);
                if (hookDll is null)
                {
                    return GameLaunchResult.Failure(
                        "Built-in locale hook DLL not found.",
                        $"Expected '{LocaleHookLibrary}' next to launcher."
                    );
                }

                var injected = WindowsLocaleInjector.TryLaunchWithHook(executable, gameArgs, gameDirectory, hookDll);
                return injected;
            }

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
