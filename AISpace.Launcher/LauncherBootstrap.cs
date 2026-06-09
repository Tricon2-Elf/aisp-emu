namespace AISpace.Launcher;

internal static class LauncherBootstrap
{
    public static LauncherSettings Settings { get; private set; } = null!;

    public static void Initialize() => Settings = LauncherSettings.LoadOrCreate();
}
